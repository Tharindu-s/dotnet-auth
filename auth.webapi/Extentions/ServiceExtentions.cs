using System.Security.Cryptography;
using auth.webapi.Data;
using auth.webapi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace auth.webapi.Extentions
{
    public static class ServiceExtentions
    {
        public static void ConfigureDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        }

        public static void ConfigureIdentity(this IServiceCollection services)
        {
            services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            services.Configure<DataProtectionTokenProviderOptions>(options =>
                options.TokenLifespan = TimeSpan.FromHours(2));
        }

        public static void ConfigureJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            // Check if the key is there and load it
            var privateKeyPath = Path.Combine(AppContext.BaseDirectory, "private_key.pem");
            if (!File.Exists(privateKeyPath))
            {
                var rsaGen = RSA.Create(2048); // new 2048 bit RSA key
                var privateKey = rsaGen.ExportRSAPrivateKey();
                File.WriteAllBytes(privateKeyPath, privateKey); // save the private key
                Console.WriteLine($"Generated new RSA private key at {privateKeyPath}");
            }

            // load the key
            var privateKeyBytes = File.ReadAllBytes(privateKeyPath);
            var rsa = System.Security.Cryptography.RSA.Create();
            rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
            var keyId = configuration["JWT:KeyId"] ?? "dev-key";
            var rsaKey = new Microsoft.IdentityModel.Tokens.RsaSecurityKey(rsa)
            {
                KeyId = keyId
            };

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                options.DefaultChallengeScheme =
                options.DefaultForbidScheme =
                options.DefaultScheme =
                options.DefaultSignInScheme =
                options.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = configuration["JWT:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["JWT:Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = rsaKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });
        }

        public static void ConfigureCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins", builder =>
                    builder.WithOrigins("http://localhost:3000")
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials()
                           );
            });
        }
    }
}