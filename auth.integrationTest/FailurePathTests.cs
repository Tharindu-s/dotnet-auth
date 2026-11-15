using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;

namespace auth.integrationTest
{
    [Collection("api")]
    public class FailurePathTests
    {
        private readonly AuthApiFactory _factory;
        private readonly ApiClient _api;

        public FailurePathTests(AuthApiFactory factory)
        {
            _factory = factory;
            _api = new ApiClient(factory.CreateApiClient());
        }

        [Fact]
        public async Task WrongApiKey_Returns401()
        {
            var app = await _api.CreateAppAsync();

            var result = await _api.RegisterAsync(app with { RawApiKey = "wrong" }, ApiClient.UniqueEmail());

            Assert.Equal(HttpStatusCode.Unauthorized, result.Status);
            Assert.Contains("Application client", result.Body.GetProperty("message").GetString());
        }

        [Fact]
        public async Task MissingAppHeaders_Returns401()
        {
            var result = await _api.SendAuthAsync(HttpMethod.Post, "/api/auth/login", null,
                new { email = "a@example.com", password = ApiClient.Password });

            Assert.Equal(HttpStatusCode.Unauthorized, result.Status);
        }

        [Fact]
        public async Task DuplicateEmail_Returns409_EvenWithDifferentCase()
        {
            var app = await _api.CreateAppAsync();
            var email = ApiClient.UniqueEmail();
            await _api.RegisterAsync(app, email);

            var result = await _api.RegisterAsync(app, email.ToUpperInvariant());

            Assert.Equal(HttpStatusCode.Conflict, result.Status);
        }

        [Fact]
        public async Task SameEmail_InTwoApps_BothSucceed()
        {
            var appA = await _api.CreateAppAsync();
            var appB = await _api.CreateAppAsync();
            var email = ApiClient.UniqueEmail();

            Assert.Equal(HttpStatusCode.OK, (await _api.RegisterAsync(appA, email)).Status);
            Assert.Equal(HttpStatusCode.OK, (await _api.RegisterAsync(appB, email)).Status);
        }

        [Fact]
        public async Task MixedCaseEmail_CanLogIn()
        {
            var app = await _api.CreateAppAsync();
            var email = $"Mixed.Case-{Guid.NewGuid():N}@Example.com";
            await _api.RegisterAsync(app, email);

            var result = await _api.LoginAsync(app, email.ToLowerInvariant());

            Assert.Equal(HttpStatusCode.OK, result.Status);
        }

        [Fact]
        public async Task WrongPassword_Returns401_ThenLocksAfterFiveFailures()
        {
            var app = await _api.CreateAppAsync();
            var email = ApiClient.UniqueEmail();
            await _api.RegisterAsync(app, email);

            for (var i = 0; i < 4; i++)
                Assert.Equal(HttpStatusCode.Unauthorized, (await _api.LoginAsync(app, email, "Wr0ng!pass")).Status);

            Assert.Equal(HttpStatusCode.Locked, (await _api.LoginAsync(app, email, "Wr0ng!pass")).Status);
            Assert.Equal(HttpStatusCode.Locked, (await _api.LoginAsync(app, email)).Status);
        }

        [Fact]
        public async Task WeakPassword_Returns400WithReason()
        {
            var app = await _api.CreateAppAsync();

            var result = await _api.RegisterAsync(app, ApiClient.UniqueEmail(), "password");

            Assert.Equal(HttpStatusCode.BadRequest, result.Status);
            Assert.Contains("digit", result.Body.GetProperty("message").GetString());
        }

        [Fact]
        public async Task CreateApp_WithoutAdminKey_Returns401()
        {
            var http = _factory.CreateApiClient();

            var response = await http.PostAsJsonAsync("/api/application-client", new { name = "Nope" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Logout_WithAnotherAppsCredentials_KeepsTheSession()
        {
            var app = await _api.CreateAppAsync();
            var otherApp = await _api.CreateAppAsync();
            var registered = await _api.RegisterAsync(app, ApiClient.UniqueEmail());

            await _api.LogoutAsync(otherApp, registered.AccessToken!, registered.RefreshToken);

            Assert.Equal(HttpStatusCode.OK, (await _api.RefreshAsync(registered.RefreshToken)).Status);
        }

        [Fact]
        public async Task Sessions_WithoutToken_Returns401()
        {
            var http = _factory.CreateApiClient();

            Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("/api/auth/sessions")).StatusCode);
        }

        [Fact]
        public async Task Login_IsRateLimitedPerIp()
        {
            var limited = _factory.WithWebHostBuilder(b => b.UseSetting("RateLimiting:Credentials:PermitLimit", "3"));
            var api = new ApiClient(limited.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false }));
            var app = await api.CreateAppAsync();

            var statuses = new List<HttpStatusCode>();
            for (var i = 0; i < 5; i++)
                statuses.Add((await api.LoginAsync(app, "nobody@example.com")).Status);

            Assert.Equal(3, statuses.Count(s => s == HttpStatusCode.Unauthorized));
            Assert.Equal(2, statuses.Count(s => s == HttpStatusCode.TooManyRequests));
        }
    }
}
