using System.Security.Cryptography;
using auth.webapi.Data;
using auth.webapi.Extentions;
using auth.webapi.Interfaces;
using auth.webapi.Models;
using auth.webapi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace auth.unitTest
{
    // Real services over an in-memory SQLite database. Only email is mocked.
    // Each instance gets its own database, so tests don't share state.
    // Every property access opens a new DI scope (a new DbContext), like a separate HTTP request.
    public sealed class TestServices : IAsyncDisposable
    {
        public const string Issuer = "https://auth.test";
        public const string AuthAudience = "https://auth.test";
        public const string ArgusOrigin = "https://argus.test";
        public const string AdminEmail = "admin@argus.test";

        private static readonly RsaSecurityKey SharedKey = CreateKey();

        private readonly SqliteConnection _connection;
        private readonly bool _ownsConnection;
        private readonly Dictionary<string, string?> _settings;
        private readonly ServiceProvider _root;
        private readonly List<AsyncServiceScope> _scopes = new();

        public Mock<IEmailService> Email { get; } = new();
        public ApplicationDbContext Db => Get<ApplicationDbContext>();
        public IAuthService Auth => Get<IAuthService>();
        public ITokenService Tokens => Get<ITokenService>();
        public IApplicationClientService Apps => Get<IApplicationClientService>();
        public IAppUserAdminService Admin => Get<IAppUserAdminService>();
        public ArgusBootstrapper Bootstrapper => Get<ArgusBootstrapper>();
        public UserManager<AppUser> Users => Get<UserManager<AppUser>>();

        private TestServices(Dictionary<string, string?>? settings, SqliteConnection? sharedConnection = null)
        {
            _settings = settings ?? new();
            _ownsConnection = sharedConnection == null;
            _connection = sharedConnection ?? new SqliteConnection("DataSource=:memory:");
            if (_ownsConnection)
                _connection.Open();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JWT:Issuer"] = Issuer,
                    ["JWT:Audience"] = AuthAudience,
                    ["Security:ApiKeySalt"] = "unit-test-salt",
                    ["Argus:AllowedOrigins:0"] = ArgusOrigin,
                    ["Argus:AdminEmail"] = AdminEmail
                })
                .AddInMemoryCollection(_settings)
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDataProtection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
            services.ConfigureIdentity();
            services.AddScoped<IConfigurationHelperService, ConfigurationHelper>();
            services.AddSingleton<ISigningKeyService>(new FixedSigningKey(SharedKey));
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IApplicationClientService, ApplicationClientService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IAppUserAdminService, AppUserAdminService>();
            services.ConfigureArgus();
            services.AddSingleton(Email.Object);

            _root = services.BuildServiceProvider();
        }

        // settings override the defaults above, such as ["Argus:AdminPassword"] = "..."
        public static async Task<TestServices> CreateAsync(Dictionary<string, string?>? settings = null)
        {
            var services = new TestServices(settings);
            await services.Db.Database.EnsureCreatedAsync();
            return services;
        }

        // Another container over the same database with extra settings, as if the service restarted with a changed config.
        // Dispose it before this one.
        public Task<TestServices> WithSettingsAsync(Dictionary<string, string?> settings)
        {
            var merged = new Dictionary<string, string?>(_settings);
            foreach (var (key, value) in settings)
                merged[key] = value;
            return Task.FromResult(new TestServices(merged, _connection));
        }

        public T Get<T>() where T : notnull
        {
            var scope = _root.CreateAsyncScope();
            _scopes.Add(scope);
            return scope.ServiceProvider.GetRequiredService<T>();
        }

        public RsaSecurityKey SigningKey => SharedKey;

        public async Task<ApplicationClient> CreateAppAsync(string name = "Test App", string? audience = null, bool registrationEnabled = true)
        {
            var created = await Apps.RegisterApplication(new() { Name = name, Audience = audience, RegistrationEnabled = registrationEnabled });
            return await Db.ApplicationClient.AsNoTracking().SingleAsync(a => a.Id == created.Id);
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var scope in _scopes)
                await scope.DisposeAsync();
            await _root.DisposeAsync();
            if (_ownsConnection)
                await _connection.DisposeAsync();
        }

        private static RsaSecurityKey CreateKey()
        {
            var key = new RsaSecurityKey(RSA.Create(2048));
            key.KeyId = Base64UrlEncoder.Encode(key.ComputeJwkThumbprint());
            return key;
        }

        private sealed class FixedSigningKey(RsaSecurityKey key) : ISigningKeyService
        {
            public RsaSecurityKey Key { get; } = key;
        }
    }
}
