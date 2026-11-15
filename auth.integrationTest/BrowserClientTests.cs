using System.Net;

namespace auth.integrationTest
{
    // Endpoints and app types the React frontend relies on
    [Collection("api")]
    public class BrowserClientTests
    {
        private const string SpaOrigin = "http://localhost:5173";

        private readonly ApiClient _api;

        public BrowserClientTests(AuthApiFactory factory)
        {
            _api = new ApiClient(factory.CreateApiClient());
        }

        [Fact]
        public async Task PublicClient_FromAllowedOrigin_CanRegisterLoginAndLogout()
        {
            var spa = await _api.CreatePublicAppAsync(SpaOrigin);
            Assert.Null(spa.RawApiKey);
            var email = ApiClient.UniqueEmail();

            var registered = await _api.RegisterAsync(spa, email);
            Assert.Equal(HttpStatusCode.OK, registered.Status);

            var loggedIn = await _api.LoginAsync(spa, email);
            Assert.Equal(HttpStatusCode.OK, loggedIn.Status);

            var logout = await _api.LogoutAsync(spa, loggedIn.AccessToken!, loggedIn.RefreshToken);
            Assert.Equal(HttpStatusCode.NoContent, logout.Status);
        }

        [Fact]
        public async Task PublicClient_FromOtherOrigin_Returns401()
        {
            var spa = await _api.CreatePublicAppAsync(SpaOrigin);

            var result = await _api.LoginAsync(spa with { Origin = "https://evil.example" }, ApiClient.UniqueEmail());

            Assert.Equal(HttpStatusCode.Unauthorized, result.Status);
        }

        [Fact]
        public async Task PublicClient_WithoutOrigin_Returns401()
        {
            var spa = await _api.CreatePublicAppAsync(SpaOrigin);

            var result = await _api.RegisterAsync(spa with { Origin = null }, ApiClient.UniqueEmail());

            Assert.Equal(HttpStatusCode.Unauthorized, result.Status);
        }

        [Fact]
        public async Task Me_ReturnsProfileAfterRefresh()
        {
            var spa = await _api.CreatePublicAppAsync(SpaOrigin);
            var email = ApiClient.UniqueEmail();
            var registered = await _api.RegisterAsync(spa, email);

            // What the frontend does on page load: cookie -> access token -> profile
            var refreshed = await _api.RefreshAsync(registered.RefreshToken);
            var me = await _api.MeAsync(refreshed.AccessToken!);

            Assert.Equal(HttpStatusCode.OK, me.Status);
            Assert.Equal(email, me.Body.GetProperty("email").GetString());
            Assert.Equal(spa.Id, me.Body.GetProperty("appId").GetGuid());
            Assert.Equal("User", me.Body.GetProperty("roles")[0].GetString());
        }

        [Fact]
        public async Task Sessions_FlagCurrent_AndRevokeOtherDevice()
        {
            var spa = await _api.CreatePublicAppAsync(SpaOrigin);
            var email = ApiClient.UniqueEmail();
            var laptop = await _api.RegisterAsync(spa, email);
            var phone = await _api.LoginAsync(spa, email);

            var sessions = await _api.SessionsAsync(laptop.AccessToken!, laptop.RefreshToken);
            var list = sessions.Body.EnumerateArray().ToList();
            Assert.Equal(2, list.Count);
            var current = Assert.Single(list, s => s.GetProperty("isCurrent").GetBoolean());
            var other = Assert.Single(list, s => !s.GetProperty("isCurrent").GetBoolean());

            var revoke = await _api.RevokeSessionAsync(laptop.AccessToken!, other.GetProperty("tokenId").GetInt32());
            Assert.Equal(HttpStatusCode.NoContent, revoke.Status);

            Assert.Equal(HttpStatusCode.Unauthorized, (await _api.RefreshAsync(phone.RefreshToken)).Status);
            Assert.Equal(HttpStatusCode.OK, (await _api.RefreshAsync(laptop.RefreshToken)).Status);

            // Revoking it again, or someone else's session, is a 404
            Assert.Equal(HttpStatusCode.NotFound, (await _api.RevokeSessionAsync(laptop.AccessToken!, other.GetProperty("tokenId").GetInt32())).Status);
        }
    }
}
