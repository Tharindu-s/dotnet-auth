using auth.webapi.Helpers;
using Microsoft.EntityFrameworkCore;

namespace auth.unitTest.Auth
{
    public class ApplicationClientServiceTests : IAsyncLifetime
    {
        private TestServices _t = null!;

        public async Task InitializeAsync() => _t = await TestServices.CreateAsync();
        public async Task DisposeAsync() => await _t.DisposeAsync();

        [Fact]
        public async Task Register_StoresHashNotRawKey()
        {
            var created = await _t.Apps.RegisterApplication(new() { Name = "App" });

            var stored = await _t.Db.ApplicationClient.SingleAsync(a => a.Id == created.Id);
            Assert.NotEqual(created.RawApiKey, stored.ApiKeyHash);
            Assert.Equal(_t.Apps.HashApiKey(created.RawApiKey!), stored.ApiKeyHash);
        }

        [Fact]
        public async Task Register_DefaultsAudienceToAppId()
        {
            var created = await _t.Apps.RegisterApplication(new() { Name = "App" });

            Assert.Equal(created.Id.ToString(), created.Audience);
        }

        [Fact]
        public async Task Register_UsesGivenAudience()
        {
            var created = await _t.Apps.RegisterApplication(new() { Name = "App", Audience = " notes-api " });

            Assert.Equal("notes-api", created.Audience);
        }

        [Fact]
        public async Task Authenticate_CorrectKey_ReturnsApp()
        {
            var created = await _t.Apps.RegisterApplication(new() { Name = "App" });

            var app = await _t.Apps.AuthenticateAsync(created.Id, created.RawApiKey, null);

            Assert.Equal(created.Id, app.Id);
        }

        [Fact]
        public async Task Authenticate_WrongKey_Throws()
        {
            var created = await _t.Apps.RegisterApplication(new() { Name = "App" });

            await Assert.ThrowsAsync<ApplicationClientAuthenticationException>(() => _t.Apps.AuthenticateAsync(created.Id, "wrong", null));
        }

        [Fact]
        public async Task Authenticate_UnknownApp_Throws()
        {
            await Assert.ThrowsAsync<ApplicationClientAuthenticationException>(() => _t.Apps.AuthenticateAsync(Guid.NewGuid(), "anything", null));
        }

        // Public clients

        [Fact]
        public async Task RegisterPublic_HasNoKey_AndNormalizesOrigins()
        {
            var created = await _t.Apps.RegisterApplication(new()
            {
                Name = "SPA",
                IsPublic = true,
                AllowedOrigins = new() { "HTTP://LocalHost:5173/", "http://localhost:5173" }
            });

            Assert.Null(created.RawApiKey);
            Assert.Equal(new[] { "http://localhost:5173" }, created.AllowedOrigins);
            Assert.Null((await _t.Db.ApplicationClient.SingleAsync(a => a.Id == created.Id)).ApiKeyHash);
        }

        [Fact]
        public async Task RegisterPublic_WithoutOrigins_Throws()
        {
            await Assert.ThrowsAsync<AppException>(() => _t.Apps.RegisterApplication(new() { Name = "SPA", IsPublic = true }));
        }

        [Theory]
        [InlineData("localhost:5173")]
        [InlineData("ftp://example.com")]
        [InlineData("https://example.com/path")]
        public async Task Register_InvalidOrigin_Throws(string origin)
        {
            await Assert.ThrowsAsync<AppException>(() =>
                _t.Apps.RegisterApplication(new() { Name = "SPA", IsPublic = true, AllowedOrigins = new() { origin } }));
        }

        [Fact]
        public async Task AuthenticatePublic_AllowedOrigin_ReturnsApp()
        {
            var created = await _t.Apps.RegisterApplication(new() { Name = "SPA", IsPublic = true, AllowedOrigins = new() { "http://localhost:5173" } });

            var app = await _t.Apps.AuthenticateAsync(created.Id, null, "http://localhost:5173");

            Assert.True(app.IsPublic);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("https://evil.example")]
        [InlineData("http://localhost:5174")]
        public async Task AuthenticatePublic_OtherOrMissingOrigin_Throws(string? origin)
        {
            var created = await _t.Apps.RegisterApplication(new() { Name = "SPA", IsPublic = true, AllowedOrigins = new() { "http://localhost:5173" } });

            await Assert.ThrowsAsync<ApplicationClientAuthenticationException>(() => _t.Apps.AuthenticateAsync(created.Id, null, origin));
        }

        [Fact]
        public async Task AuthenticateConfidential_IgnoresOrigin_StillNeedsKey()
        {
            var created = await _t.Apps.RegisterApplication(new() { Name = "Server", AllowedOrigins = new() { "http://localhost:5173" } });

            await Assert.ThrowsAsync<ApplicationClientAuthenticationException>(() => _t.Apps.AuthenticateAsync(created.Id, null, "http://localhost:5173"));
        }
    }
}
