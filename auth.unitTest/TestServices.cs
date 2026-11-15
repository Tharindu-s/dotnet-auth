using System.Security.Cryptography;
using auth.webapi.Data;
using auth.webapi.Extentions;
using auth.webapi.Interfaces;
using auth.webapi.Models;
using auth.webapi.Services;
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

        private static readonly RsaSecurityKey SharedKey = CreateKey();

        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _root;
        private readonly List<AsyncServiceScope> _scopes = new();

        public Mock<IEmailService> Email { get; } = new();
        public ApplicationDbContext Db => Get<ApplicationDbContext>();
        public IAuthService Auth => Get<IAuthService>();
        public ITokenService Tokens => Get<ITokenService>();
        public IApplicationClientService Apps => Get<IApplicationClientService>();

        private TestServices()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JWT:Issuer"] = Issuer,
                    ["JWT:Audience"] = AuthAudience,
                    ["Security:ApiKeySalt"] = "unit-test-salt"
                })
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
            services.AddSingleton(Email.Object);

            _root = services.BuildServiceProvider();
        }

        public static async Task<TestServices> CreateAsync()
        {
            var services = new TestServices();
            await services.Db.Database.EnsureCreatedAsync();
            return services;
        }

        public T Get<T>() where T : notnull
        {
            var scope = _root.CreateAsyncScope();
            _scopes.Add(scope);
            return scope.ServiceProvider.GetRequiredService<T>();
        }

        public RsaSecurityKey SigningKey => SharedKey;

        public async Task<ApplicationClient> CreateAppAsync(string name = "Test App", string? audience = null)
        {
            var created = await Apps.RegisterApplication(new() { Name = name, Audience = audience });
            return await Db.ApplicationClient.AsNoTracking().SingleAsync(a => a.Id == created.Id);
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var scope in _scopes)
                await scope.DisposeAsync();
            await _root.DisposeAsync();
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
