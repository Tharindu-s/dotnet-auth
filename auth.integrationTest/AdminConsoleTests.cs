using System.Net;
using System.Text.Json;
using auth.webapi.Models;
using Microsoft.EntityFrameworkCore;

namespace auth.integrationTest
{
    // The Argus admin console: its app, admin access rules, and the apps API end to end
    [Collection("api")]
    public class AdminConsoleTests
    {
        private readonly AuthApiFactory _factory;
        private readonly ApiClient _api;

        public AdminConsoleTests(AuthApiFactory factory)
        {
            _factory = factory;
            _api = new ApiClient(factory.CreateApiClient());
        }

        // Access rules

        [Fact]
        public async Task Config_IsPublic_AndNamesTheArgusApp()
        {
            var argus = await _api.ArgusAppAsync();

            Guid systemAppId = default;
            await _factory.WithDbAsync(async db => systemAppId = (await db.ApplicationClient.SingleAsync(a => a.IsSystem)).Id);
            Assert.Equal(systemAppId, argus.Id);
        }

        [Fact]
        public async Task Register_IntoTheArgusApp_Returns403()
        {
            var argus = await _api.ArgusAppAsync();

            var result = await _api.RegisterAsync(argus, ApiClient.UniqueEmail());

            Assert.Equal(HttpStatusCode.Forbidden, result.Status);
            Assert.Null(result.RefreshToken);
        }

        [Fact]
        public async Task AppsApi_WithAnAdminFromAnotherApp_Returns403()
        {
            // An "Admin" in one of your own apps must not be able to manage Argus
            var app = await _api.CreateAppAsync();
            var email = ApiClient.UniqueEmail();
            var registered = await _api.RegisterAsync(app, email);
            await _factory.WithUsersAsync(async users =>
                await users.AddToRoleAsync((await users.FindByIdAsync(registered.Body.GetProperty("id").GetString()!))!, "Admin"));
            var login = await _api.LoginAsync(app, email);

            var result = await _api.SendAuthAsync(HttpMethod.Get, "/api/apps", null, null, login.AccessToken);

            Assert.Equal(HttpStatusCode.Forbidden, result.Status);
        }

        [Fact]
        public async Task AppsApi_BeforeThePasswordIsChanged_Returns403_ThenWorks()
        {
            var argus = await _api.ArgusAppAsync();
            var email = ApiClient.UniqueEmail();
            await _factory.WithUsersAsync(async users =>
            {
                var admin = new AppUser { Email = email, UserName = email, FullName = "New admin", City = "", ApplicationClientId = argus.Id, MustChangePassword = true };
                Assert.True((await users.CreateAsync(admin, ApiClient.Password)).Succeeded);
                await users.AddToRoleAsync(admin, "Admin");
            });
            var login = await _api.LoginAsync(argus, email);

            var before = await _api.SendAuthAsync(HttpMethod.Get, "/api/apps", null, null, login.AccessToken);
            var me = await _api.MeAsync(login.AccessToken!);
            var change = await _api.SendAuthAsync(HttpMethod.Post, "/api/auth/change-password", null,
                new { currentPassword = ApiClient.Password, newPassword = "N3w-Passw0rd!" }, login.AccessToken, login.RefreshToken);
            var after = await _api.SendAuthAsync(HttpMethod.Get, "/api/apps", null, null, login.AccessToken);

            Assert.Equal(HttpStatusCode.Forbidden, before.Status);
            Assert.True(me.Body.GetProperty("mustChangePassword").GetBoolean());
            Assert.Equal(HttpStatusCode.NoContent, change.Status);
            Assert.Equal(HttpStatusCode.OK, after.Status);
            // The session that changed the password stays signed in
            Assert.Equal(HttpStatusCode.OK, (await _api.RefreshAsync(login.RefreshToken)).Status);
        }

        [Fact]
        public async Task ArgusApp_IsHiddenFromTheAppsApi()
        {
            var argus = await _api.ArgusAppAsync();

            var list = await _api.AdminAsync(HttpMethod.Get, "/api/apps");
            var get = await _api.AdminAsync(HttpMethod.Get, $"/api/apps/{argus.Id}");
            var delete = await _api.AdminAsync(HttpMethod.Delete, $"/api/apps/{argus.Id}");

            Assert.DoesNotContain(list.Body.EnumerateArray(), a => a.GetProperty("id").GetGuid() == argus.Id);
            Assert.Equal(HttpStatusCode.NotFound, get.Status);
            Assert.Equal(HttpStatusCode.NotFound, delete.Status);
        }

        // Apps API

        [Fact]
        public async Task AppLifecycle_CreateUpdateRotateDelete()
        {
            var app = await _api.CreateAppAsync("lifecycle-api");
            Assert.NotNull(app.RawApiKey);

            var list = await _api.AdminAsync(HttpMethod.Get, "/api/apps");
            Assert.Contains(list.Body.EnumerateArray(), a => a.GetProperty("id").GetGuid() == app.Id);

            // Turning sign-ups off stops registration
            var update = await _api.AdminAsync(HttpMethod.Put, $"/api/apps/{app.Id}",
                new { name = "Renamed", audience = "lifecycle-api", allowedOrigins = Array.Empty<string>(), registrationEnabled = false });
            Assert.Equal(HttpStatusCode.OK, update.Status);
            Assert.Equal("Renamed", update.Body.GetProperty("name").GetString());
            Assert.Equal(HttpStatusCode.Forbidden, (await _api.RegisterAsync(app, ApiClient.UniqueEmail())).Status);

            // A rotated key replaces the old one immediately
            var rotate = await _api.AdminAsync(HttpMethod.Post, $"/api/apps/{app.Id}/rotate-key");
            var rotated = app with { RawApiKey = rotate.Body.GetProperty("rawApiKey").GetString() };
            // The old key fails app authentication (401); the new one passes it and reaches the sign-ups check (403)
            Assert.Equal(HttpStatusCode.Unauthorized, (await _api.RegisterAsync(app, ApiClient.UniqueEmail())).Status);
            Assert.Equal(HttpStatusCode.Forbidden, (await _api.RegisterAsync(rotated, ApiClient.UniqueEmail())).Status);

            // Deleting the app ends it
            Assert.Equal(HttpStatusCode.NoContent, (await _api.AdminAsync(HttpMethod.Delete, $"/api/apps/{app.Id}")).Status);
            Assert.Equal(HttpStatusCode.NotFound, (await _api.AdminAsync(HttpMethod.Get, $"/api/apps/{app.Id}")).Status);
            Assert.Equal(HttpStatusCode.Unauthorized, (await _api.LoginAsync(rotated, "nobody@example.com")).Status);
        }

        [Fact]
        public async Task Users_ListLockUnlockDelete()
        {
            var app = await _api.CreateAppAsync();
            var email = ApiClient.UniqueEmail();
            var registered = await _api.RegisterAsync(app, email);
            var userId = registered.Body.GetProperty("id").GetString();

            var users = await _api.AdminAsync(HttpMethod.Get, $"/api/apps/{app.Id}/users?search={Uri.EscapeDataString(email)}");
            var listed = Assert.Single(users.Body.GetProperty("items").EnumerateArray());
            Assert.Equal(userId, listed.GetProperty("id").GetString());
            Assert.Equal(1, listed.GetProperty("activeSessionCount").GetInt32());

            // Lock: no sign-in, and existing sessions end
            Assert.Equal(HttpStatusCode.NoContent, (await _api.AdminAsync(HttpMethod.Post, $"/api/apps/{app.Id}/users/{userId}/lock")).Status);
            Assert.Equal(HttpStatusCode.Locked, (await _api.LoginAsync(app, email)).Status);
            Assert.Equal(HttpStatusCode.Unauthorized, (await _api.RefreshAsync(registered.RefreshToken)).Status);

            Assert.Equal(HttpStatusCode.NoContent, (await _api.AdminAsync(HttpMethod.Post, $"/api/apps/{app.Id}/users/{userId}/unlock")).Status);
            Assert.Equal(HttpStatusCode.OK, (await _api.LoginAsync(app, email)).Status);

            Assert.Equal(HttpStatusCode.NoContent, (await _api.AdminAsync(HttpMethod.Delete, $"/api/apps/{app.Id}/users/{userId}")).Status);
            var after = await _api.AdminAsync(HttpMethod.Get, $"/api/apps/{app.Id}/users");
            Assert.Equal(0, after.Body.GetProperty("total").GetInt32());
            Assert.Equal(HttpStatusCode.Unauthorized, (await _api.LoginAsync(app, email)).Status);
        }

        [Fact]
        public async Task Sessions_ListAndRevoke()
        {
            var app = await _api.CreateAppAsync();
            var email = ApiClient.UniqueEmail();
            var first = await _api.RegisterAsync(app, email);
            var second = await _api.LoginAsync(app, email);

            var sessions = await _api.AdminAsync(HttpMethod.Get, $"/api/apps/{app.Id}/sessions");
            Assert.Equal(2, sessions.Body.GetArrayLength());
            Assert.All(sessions.Body.EnumerateArray(), s => Assert.Equal(email, s.GetProperty("userEmail").GetString()));

            // Newest first, so this is the second login
            var newest = sessions.Body[0].GetProperty("tokenId").GetInt32();
            Assert.Equal(HttpStatusCode.NoContent, (await _api.AdminAsync(HttpMethod.Delete, $"/api/apps/{app.Id}/sessions/{newest}")).Status);

            Assert.Equal(HttpStatusCode.Unauthorized, (await _api.RefreshAsync(second.RefreshToken)).Status);
            Assert.Equal(HttpStatusCode.OK, (await _api.RefreshAsync(first.RefreshToken)).Status);
        }

        [Fact]
        public async Task CreatePublicApp_WithoutOrigins_Returns400()
        {
            var result = await _api.AdminAsync(HttpMethod.Post, "/api/apps", new { name = "SPA", isPublic = true });

            Assert.Equal(HttpStatusCode.BadRequest, result.Status);
            Assert.Contains("origin", result.Body.GetProperty("message").GetString());
        }
    }
}
