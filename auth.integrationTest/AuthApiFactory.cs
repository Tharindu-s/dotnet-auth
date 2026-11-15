using System.Security.Cryptography;
using auth.webapi.Data;
using auth.webapi.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace auth.integrationTest
{
    // Runs the real app against a throwaway Postgres container, with migrations applied.
    // Shared by every test class in the "api" collection; tests isolate themselves by creating their own app client.
    public class AuthApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        public const string AdminKey = "integration-admin-key";
        public const string Issuer = "https://auth.integration.test";

        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

        private readonly string _signingKeyPem = RSA.Create(2048).ExportRSAPrivateKeyPem();

        public RecordingEmailService Email { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Not "Development", so user secrets and the dev signing key file aren't used
            builder.UseEnvironment("Testing");

            builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
            builder.UseSetting("JWT:Issuer", Issuer);
            builder.UseSetting("JWT:Audience", Issuer);
            builder.UseSetting("JWT:PrivateKey", _signingKeyPem);
            builder.UseSetting("Security:ApiKeySalt", "integration-salt");
            builder.UseSetting("Security:AdminApiKey", AdminKey);
            builder.UseSetting("RateLimiting:Credentials:PermitLimit", "10000");
            builder.UseSetting("RateLimiting:Refresh:PermitLimit", "10000");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IEmailService>();
                services.AddSingleton<IEmailService>(Email);
            });
        }

        public HttpClient CreateApiClient() => CreateClient(new WebApplicationFactoryClientOptions
        {
            // The refresh cookie is Secure, and tests manage cookies themselves
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = false,
            AllowAutoRedirect = false
        });

        public async Task WithDbAsync(Func<ApplicationDbContext, Task> action)
        {
            await using var scope = Services.CreateAsyncScope();
            await action(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
        }

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
            await WithDbAsync(db => db.Database.MigrateAsync());
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await base.DisposeAsync();
            await _postgres.DisposeAsync();
        }
    }

    public class RecordingEmailService : IEmailService
    {
        private readonly List<string> _recipients = new();

        public IReadOnlyList<string> Recipients
        {
            get { lock (_recipients) return _recipients.ToList(); }
        }

        public Task SendEmailAsync(string toEmail, string subject, string message)
        {
            lock (_recipients) _recipients.Add(toEmail);
            return Task.CompletedTask;
        }
    }

    [CollectionDefinition("api")]
    public class ApiCollection : ICollectionFixture<AuthApiFactory> { }
}
