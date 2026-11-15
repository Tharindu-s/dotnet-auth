using auth.webapi.DTO.Auth.Login;
using auth.webapi.Helpers;
using auth.webapi.Services;
using Microsoft.EntityFrameworkCore;

namespace auth.unitTest.Auth
{
    public class ArgusBootstrapperTests
    {
        private const string AdminPassword = "Adm1n-Passw0rd!";

        private static Task<TestServices> CreateAsync(Dictionary<string, string?>? settings = null) =>
            TestServices.CreateAsync(settings ?? new() { ["Argus:AdminPassword"] = AdminPassword });

        [Fact]
        public async Task Run_CreatesTheArgusAppAsAPublicSystemAppWithSignUpsOff()
        {
            await using var t = await CreateAsync();

            await t.Bootstrapper.RunAsync();

            var app = await t.Db.ApplicationClient.SingleAsync(a => a.IsSystem);
            Assert.Equal("Argus", app.Name);
            Assert.True(app.IsPublic);
            Assert.False(app.RegistrationEnabled);
            Assert.Null(app.ApiKeyHash);
            Assert.Equal(new[] { TestServices.ArgusOrigin }, app.AllowedOrigins);
            Assert.Equal(TestServices.AuthAudience, app.Audience);
        }

        [Fact]
        public async Task Run_CreatesAnAdminWhoMustChangeTheirPassword()
        {
            await using var t = await CreateAsync();

            await t.Bootstrapper.RunAsync();

            var app = await t.Db.ApplicationClient.SingleAsync(a => a.IsSystem);
            var admin = await t.Db.Users.SingleAsync(u => u.ApplicationClientId == app.Id);
            Assert.Equal(TestServices.AdminEmail, admin.Email);
            Assert.True(admin.MustChangePassword);
            Assert.True(await t.Users.IsInRoleAsync(admin, ArgusOptions.AdminRole));
            Assert.True(await t.Users.CheckPasswordAsync(admin, AdminPassword));
        }

        [Fact]
        public async Task Run_IsSafeToRepeat()
        {
            await using var t = await CreateAsync();

            await t.Bootstrapper.RunAsync();
            await t.Bootstrapper.RunAsync();

            Assert.Equal(1, await t.Db.ApplicationClient.CountAsync(a => a.IsSystem));
            Assert.Equal(1, await t.Db.Users.CountAsync());
        }

        [Fact]
        public async Task Run_WithoutAConfiguredPassword_GeneratesOneThatPassesThePasswordRules()
        {
            await using var t = await CreateAsync(new());

            await t.Bootstrapper.RunAsync();

            var admin = await t.Db.Users.SingleAsync();
            Assert.NotNull(admin.PasswordHash);
        }

        [Fact]
        public async Task Run_WithAPasswordThatBreaksTheRules_FailsWithAClearMessage()
        {
            await using var t = await CreateAsync(new() { ["Argus:AdminPassword"] = "123" });

            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => t.Bootstrapper.RunAsync());
            Assert.Contains(TestServices.AdminEmail, error.Message);
        }

        [Fact]
        public async Task Run_WithNoAllowedOrigins_Fails()
        {
            await using var t = await CreateAsync(new() { ["Argus:AllowedOrigins:0"] = "" });

            await Assert.ThrowsAsync<InvalidOperationException>(() => t.Bootstrapper.RunAsync());
        }

        [Fact]
        public async Task Run_AppliesTheConfiguredAllowedOriginsToTheExistingApp()
        {
            await using var t = await CreateAsync();
            await t.Bootstrapper.RunAsync();
            var original = await t.Db.ApplicationClient.AsNoTracking().SingleAsync(a => a.IsSystem);

            await using var restarted = await t.WithSettingsAsync(new() { ["Argus:AllowedOrigins:0"] = "https://console.example.com" });
            await restarted.Bootstrapper.RunAsync();

            var updated = await t.Db.ApplicationClient.AsNoTracking().SingleAsync(a => a.IsSystem);
            Assert.Equal(original.Id, updated.Id);
            Assert.Equal(new[] { "https://console.example.com" }, updated.AllowedOrigins);
        }

        [Fact]
        public async Task Run_WithResetAdminPassword_ResetsAndUnlocksTheAdmin()
        {
            await using var t = await CreateAsync();
            await t.Bootstrapper.RunAsync();
            var adminId = (await t.Db.Users.SingleAsync()).Id;

            // The admin changed their password, then locked themselves out
            await t.Auth.ChangePasswordAsync(adminId, new() { CurrentPassword = AdminPassword, NewPassword = "N3w-Passw0rd!" }, null);
            var users = t.Users;
            await users.SetLockoutEndDateAsync((await users.FindByIdAsync(adminId))!, DateTimeOffset.UtcNow.AddHours(1));

            await using var recovery = await t.WithSettingsAsync(new() { ["Argus:AdminPassword"] = "R3set-Passw0rd!", ["Argus:ResetAdminPassword"] = "true" });
            await recovery.Bootstrapper.RunAsync();

            var admin = (await t.Users.FindByIdAsync(adminId))!;
            Assert.True(admin.MustChangePassword);
            Assert.False(await t.Users.IsLockedOutAsync(admin));
            Assert.True(await t.Users.CheckPasswordAsync(admin, "R3set-Passw0rd!"));
        }

        [Fact]
        public async Task ArgusApp_DoesNotAllowSignUps()
        {
            await using var t = await CreateAsync();
            await t.Bootstrapper.RunAsync();
            var app = await t.Db.ApplicationClient.AsNoTracking().SingleAsync(a => a.IsSystem);

            await Assert.ThrowsAsync<RegistrationDisabledException>(() => t.Auth.RegisterUserAsync(
                new() { FullName = "Mallory", Email = "mallory@example.com", City = "Nowhere", Password = "Passw0rd!" }, app, "127.0.0.1", "test"));
        }

        [Fact]
        public async Task Admin_CanSignInToTheArgusApp()
        {
            await using var t = await CreateAsync();
            await t.Bootstrapper.RunAsync();
            var app = await t.Db.ApplicationClient.AsNoTracking().SingleAsync(a => a.IsSystem);

            var user = await t.Auth.LoginUserAsync(new LoginUserDto { Email = TestServices.AdminEmail, Password = AdminPassword }, app, "127.0.0.1", "test");

            Assert.False(string.IsNullOrEmpty(user.AccessToken));
        }
    }
}
