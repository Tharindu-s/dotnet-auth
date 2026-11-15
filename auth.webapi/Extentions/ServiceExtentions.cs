using System.Text.Json;
using System.Threading.RateLimiting;
using auth.webapi.Data;
using auth.webapi.Interfaces;
using auth.webapi.Models;
using auth.webapi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace auth.webapi.Extentions
{
    // Settings are read from IConfiguration when the options are first used, not while
    // services are registered, so tests and environment variables can override them.
    public static class ServiceExtentions
    {
        public const string FrontendCorsPolicy = "Frontend";
        public const string CredentialsRateLimitPolicy = "credentials";
        public const string RefreshRateLimitPolicy = "refresh";

        public static void ConfigureDatabase(this IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
                options.UseNpgsql(serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")));
        }

        public static void ConfigureIdentity(this IServiceCollection services)
        {
            services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // Usernames are unique per app, not globally (see ApplicationDbContext)
            services.RemoveAll<IUserValidator<AppUser>>();
            services.AddScoped<IUserValidator<AppUser>, AppScopedUserValidator>();

            services.Configure<DataProtectionTokenProviderOptions>(options =>
                options.TokenLifespan = TimeSpan.FromHours(2));
        }

        // The Argus admin console: its settings and the startup step that creates its app and first admin
        public static void ConfigureArgus(this IServiceCollection services)
        {
            services.AddOptions<ArgusOptions>().BindConfiguration("Argus");
            services.AddScoped<ArgusBootstrapper>();
        }

        // Run after the app is built and before it starts listening
        public static async Task RunStartupTasksAsync(this IServiceProvider services)
        {
            await using var scope = services.CreateAsyncScope();
            var provider = scope.ServiceProvider;

            if (provider.GetRequiredService<IConfiguration>().GetValue<bool>("Database:MigrateOnStartup"))
                await provider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();

            await provider.GetRequiredService<ArgusBootstrapper>().RunAsync();
        }

        public static void ConfigureJwtAuthentication(this IServiceCollection services)
        {
            services.AddSingleton<ISigningKeyService, SigningKeyService>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                options.DefaultChallengeScheme =
                options.DefaultForbidScheme =
                options.DefaultScheme =
                options.DefaultSignInScheme =
                options.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer();

            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IConfiguration, ISigningKeyService>((options, configuration, signingKey) =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = configuration["JWT:Issuer"],
                        ValidateAudience = true,
                        ValidAudience = configuration["JWT:Audience"],
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = signingKey.Key,
                        ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 },
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                });
        }

        public static void ConfigureCors(this IServiceCollection services)
        {
            services.AddCors();
            services.AddOptions<CorsOptions>()
                .Configure<IConfiguration>((options, configuration) =>
                {
                    var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
                    options.AddPolicy(FrontendCorsPolicy, policy =>
                        policy.WithOrigins(origins)
                              .AllowAnyMethod()
                              .AllowAnyHeader()
                              .AllowCredentials());
                });
        }

        // Behind nginx or a load balancer, RemoteIpAddress is the proxy's address. Rate limits and the
        // sessions list need the client's, which the proxy sends in X-Forwarded-For. Only proxies in
        // ForwardedHeaders:KnownNetworks (plus loopback) are trusted, since anyone can send that header.
        public static void ConfigureForwardedHeaders(this IServiceCollection services)
        {
            services.AddOptions<ForwardedHeadersOptions>()
                .Configure<IConfiguration>((options, configuration) =>
                {
                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                    var networks = configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? Array.Empty<string>();
                    foreach (var network in networks)
                        options.KnownNetworks.Add(Microsoft.AspNetCore.HttpOverrides.IPNetwork.Parse(network));
                });
        }

        public static void ConfigureRateLimiting(this IServiceCollection services)
        {
            services.AddOptions<RateLimitSettings>().BindConfiguration("RateLimiting");

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.ContentType = "application/json";
                    var body = JsonSerializer.Serialize(new { message = "Too many requests. Try again later." });
                    await context.HttpContext.Response.WriteAsync(body, cancellationToken);
                };

                // One fixed window per client IP and policy. Login and register share a budget; refresh has its own.
                options.AddPolicy(CredentialsRateLimitPolicy, context => PerIpWindow(context, s => s.Credentials));
                options.AddPolicy(RefreshRateLimitPolicy, context => PerIpWindow(context, s => s.Refresh));
            });
        }

        private static RateLimitPartition<string> PerIpWindow(HttpContext context, Func<RateLimitSettings, RateLimitWindow> select)
        {
            var window = select(context.RequestServices.GetRequiredService<IOptions<RateLimitSettings>>().Value);
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = window.PermitLimit,
                Window = TimeSpan.FromSeconds(window.WindowSeconds),
                QueueLimit = 0
            });
        }
    }

    public class RateLimitSettings
    {
        public RateLimitWindow Credentials { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };
        public RateLimitWindow Refresh { get; set; } = new() { PermitLimit = 30, WindowSeconds = 60 };
    }

    public class RateLimitWindow
    {
        public int PermitLimit { get; set; }
        public int WindowSeconds { get; set; }
    }
}
