using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using auth.webapi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace auth.integrationTest
{
    [Collection("api")]
    public class AuthFlowTests
    {
        private readonly AuthApiFactory _factory;
        private readonly ApiClient _api;

        public AuthFlowTests(AuthApiFactory factory)
        {
            _factory = factory;
            _api = new ApiClient(factory.CreateApiClient());
        }

        [Fact]
        public async Task FullFlow_Register_Login_Refresh_Reuse_Logout_Sessions()
        {
            var app = await _api.CreateAppAsync();
            var email = ApiClient.UniqueEmail();

            var registered = await _api.RegisterAsync(app, email);
            Assert.Equal(HttpStatusCode.OK, registered.Status);
            Assert.NotNull(registered.AccessToken);
            Assert.False(string.IsNullOrEmpty(registered.RefreshToken));
            Assert.False(registered.Body.TryGetProperty("refreshToken", out _)); // cookie only
            Assert.Contains(email, _factory.Email.Recipients);

            var loggedIn = await _api.LoginAsync(app, email);
            Assert.Equal(HttpStatusCode.OK, loggedIn.Status);

            var refreshed = await _api.RefreshAsync(loggedIn.RefreshToken);
            Assert.Equal(HttpStatusCode.OK, refreshed.Status);
            Assert.NotNull(refreshed.AccessToken);
            Assert.NotEqual(loggedIn.RefreshToken, refreshed.RefreshToken);

            // Replaying the rotated token fails
            var replayed = await _api.RefreshAsync(loggedIn.RefreshToken);
            Assert.Equal(HttpStatusCode.Unauthorized, replayed.Status);

            // Two sessions: the register one and the (rotated) login one
            var sessions = await _api.SessionsAsync(refreshed.AccessToken!);
            Assert.Equal(HttpStatusCode.OK, sessions.Status);
            Assert.Equal(2, sessions.Body.GetArrayLength());
            Assert.All(sessions.Body.EnumerateArray(), s =>
                Assert.True(s.GetProperty("issuedAt").GetDateTime() > DateTime.UtcNow.AddMinutes(-5)));

            var logout = await _api.LogoutAsync(app, refreshed.AccessToken!, refreshed.RefreshToken);
            Assert.Equal(HttpStatusCode.NoContent, logout.Status);
            Assert.Equal("", logout.RefreshToken); // cookie cleared

            Assert.Equal(HttpStatusCode.Unauthorized, (await _api.RefreshAsync(refreshed.RefreshToken)).Status);

            sessions = await _api.SessionsAsync(registered.AccessToken!);
            Assert.Equal(1, sessions.Body.GetArrayLength());
        }

        [Fact]
        public async Task Reuse_AfterGracePeriod_RevokesTheWholeFamily()
        {
            var app = await _api.CreateAppAsync();
            var registered = await _api.RegisterAsync(app, ApiClient.UniqueEmail());
            var refreshed = await _api.RefreshAsync(registered.RefreshToken);

            // Pretend the rotation happened a while ago, so a replay looks like theft rather than a race
            var longAgo = DateTime.UtcNow.AddHours(-1);
            await _factory.WithDbAsync(db => db.RefreshTokens
                .Where(rt => rt.RevokedAt != null && rt.App.Id == app.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.RevokedAt, longAgo)));

            Assert.Equal(HttpStatusCode.Unauthorized, (await _api.RefreshAsync(registered.RefreshToken)).Status);
            Assert.Equal(HttpStatusCode.Unauthorized, (await _api.RefreshAsync(refreshed.RefreshToken)).Status);
        }

        [Fact]
        public async Task ConcurrentRefreshes_ExactlyOneWins_AndSessionSurvives()
        {
            var app = await _api.CreateAppAsync();
            var registered = await _api.RegisterAsync(app, ApiClient.UniqueEmail());

            var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => _api.RefreshAsync(registered.RefreshToken)));

            var winner = Assert.Single(results, r => r.Status == HttpStatusCode.OK);
            Assert.All(results.Where(r => r != winner), r => Assert.Equal(HttpStatusCode.Unauthorized, r.Status));
            Assert.Equal(HttpStatusCode.OK, (await _api.RefreshAsync(winner.RefreshToken)).Status);
        }

        [Fact]
        public async Task ExpiredRefreshToken_Returns401()
        {
            var app = await _api.CreateAppAsync();
            var registered = await _api.RegisterAsync(app, ApiClient.UniqueEmail());

            var past = DateTime.UtcNow.AddMinutes(-1);
            await _factory.WithDbAsync(db => db.RefreshTokens
                .Where(rt => rt.App.Id == app.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.Expires, past)));

            Assert.Equal(HttpStatusCode.Unauthorized, (await _api.RefreshAsync(registered.RefreshToken)).Status);
        }

        [Fact]
        public async Task Refresh_WithoutCookie_Returns401()
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await _api.RefreshAsync(null)).Status);
        }

        [Fact]
        public async Task AccessToken_ValidatesAgainstJwks_WithAppAudienceAndClaims()
        {
            var app = await _api.CreateAppAsync("notes-api");
            var registered = await _api.RegisterAsync(app, ApiClient.UniqueEmail());

            var http = _factory.CreateApiClient();
            var jwks = new JsonWebKeySet(await http.GetStringAsync("/.well-known/jwks.json"));

            // What a downstream "notes-api" would do
            var parameters = new TokenValidationParameters
            {
                ValidIssuer = AuthApiFactory.Issuer,
                ValidAudience = "notes-api",
                IssuerSigningKeys = jwks.GetSigningKeys(),
                ClockSkew = TimeSpan.Zero
            };
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            handler.ValidateToken(registered.AccessToken, parameters, out var validated);

            var jwt = (JwtSecurityToken)validated;
            Assert.Equal(app.Id.ToString(), jwt.Claims.Single(c => c.Type == TokenService.AppIdClaimType).Value);
            Assert.Contains(jwt.Claims, c => c.Type == "role" && c.Value == "User");

            // An API for another app rejects it
            parameters.ValidAudience = "billing-api";
            Assert.Throws<SecurityTokenInvalidAudienceException>(() => handler.ValidateToken(registered.AccessToken, parameters, out _));
        }
    }
}
