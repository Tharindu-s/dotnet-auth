using auth.webapi.DTO.Auth.Login;
using auth.webapi.Helpers;
using auth.webapi.Models;
using Microsoft.EntityFrameworkCore;

namespace auth.unitTest.Auth
{
    public class AppUserAdminServiceTests : IAsyncLifetime
    {
        private const string Password = "Passw0rd!";

        private TestServices _t = null!;
        private ApplicationClient _app = null!;

        public async Task InitializeAsync()
        {
            _t = await TestServices.CreateAsync();
            _app = await _t.CreateAppAsync();
        }

        public async Task DisposeAsync() => await _t.DisposeAsync();

        private Task<auth.webapi.DTO.Auth.ResponseUserDto> RegisterAsync(string email, string fullName = "Jane Doe", ApplicationClient? app = null) =>
            _t.Auth.RegisterUserAsync(new() { FullName = fullName, Email = email, City = "Colombo", Password = Password }, app ?? _app, "127.0.0.1", "test");

        private Task<auth.webapi.DTO.Auth.ResponseUserDto> LoginAsync(string email) =>
            _t.Auth.LoginUserAsync(new LoginUserDto { Email = email, Password = Password }, _app, "127.0.0.1", "test");

        // Users

        [Fact]
        public async Task ListUsers_OnlyThisApp_SortedByEmail_WithSessionInfo()
        {
            await RegisterAsync("zoe@example.com");
            await RegisterAsync("adam@example.com");
            await RegisterAsync("other@example.com", app: await _t.CreateAppAsync("Other"));

            var result = await _t.Admin.ListUsersAsync(_app.Id, null, 1, 25);

            Assert.Equal(2, result.Total);
            Assert.Equal(new[] { "adam@example.com", "zoe@example.com" }, result.Items.Select(u => u.Email));
            Assert.All(result.Items, u =>
            {
                Assert.Equal(1, u.ActiveSessionCount);
                Assert.NotNull(u.LastSignInAt);
                Assert.False(u.IsLockedOut);
            });
        }

        [Theory]
        [InlineData("ADAM")]
        [InlineData("lovelace")]
        public async Task ListUsers_SearchMatchesEmailOrName_IgnoringCase(string search)
        {
            await RegisterAsync("adam@example.com", "Ada Lovelace");
            await RegisterAsync("zoe@example.com", "Zoe Smith");

            var result = await _t.Admin.ListUsersAsync(_app.Id, search, 1, 25);

            Assert.Equal("adam@example.com", Assert.Single(result.Items).Email);
        }

        [Fact]
        public async Task ListUsers_Pages()
        {
            for (var i = 0; i < 5; i++)
                await RegisterAsync($"user{i}@example.com");

            var page2 = await _t.Admin.ListUsersAsync(_app.Id, null, 2, 2);

            Assert.Equal(5, page2.Total);
            Assert.Equal(new[] { "user2@example.com", "user3@example.com" }, page2.Items.Select(u => u.Email));
        }

        [Fact]
        public async Task ListUsers_ArgusApp_IsNotFound()
        {
            await _t.Bootstrapper.RunAsync();
            var argusId = (await _t.Db.ApplicationClient.SingleAsync(a => a.IsSystem)).Id;

            await Assert.ThrowsAsync<ApplicationClientNotFoundException>(() => _t.Admin.ListUsersAsync(argusId, null, 1, 25));
        }

        [Fact]
        public async Task LockUser_BlocksLoginAndRevokesSessions()
        {
            var user = await RegisterAsync("jane@example.com");

            await _t.Admin.LockUserAsync(_app.Id, user.Id);

            await Assert.ThrowsAsync<AccountLockedException>(() => LoginAsync("jane@example.com"));
            await Assert.ThrowsAsync<InvalidRefreshTokenException>(() => _t.Auth.RefreshTokenAsync(user.RefreshToken, "127.0.0.1", "test"));
            Assert.True(Assert.Single((await _t.Admin.ListUsersAsync(_app.Id, null, 1, 25)).Items).IsLockedOut);
        }

        [Fact]
        public async Task UnlockUser_AllowsLoginAgain()
        {
            var user = await RegisterAsync("jane@example.com");
            await _t.Admin.LockUserAsync(_app.Id, user.Id);

            await _t.Admin.UnlockUserAsync(_app.Id, user.Id);

            await LoginAsync("jane@example.com");
        }

        [Fact]
        public async Task LockUser_FromAnotherApp_IsNotFound()
        {
            var user = await RegisterAsync("jane@example.com");
            var other = await _t.CreateAppAsync("Other");

            await Assert.ThrowsAsync<UserNotFoundException>(() => _t.Admin.LockUserAsync(other.Id, user.Id));
        }

        [Fact]
        public async Task DeleteUser_RemovesUserAndTokens()
        {
            var user = await RegisterAsync("jane@example.com");

            await _t.Admin.DeleteUserAsync(_app.Id, user.Id);

            Assert.False(await _t.Db.Users.AnyAsync());
            Assert.False(await _t.Db.RefreshTokens.AnyAsync());
        }

        // Sessions

        [Fact]
        public async Task ListSessions_ActiveOnly_FilterableByUser()
        {
            var jane = await RegisterAsync("jane@example.com");
            var john = await RegisterAsync("john@example.com");
            await LoginAsync("jane@example.com");
            await _t.Auth.LogoutAsync(john.Id, john.RefreshToken, _app);

            var all = await _t.Admin.ListSessionsAsync(_app.Id, null);
            var janes = await _t.Admin.ListSessionsAsync(_app.Id, jane.Id);

            Assert.Equal(2, all.Count);
            Assert.All(all, s => Assert.Equal("jane@example.com", s.UserEmail));
            Assert.Equal(2, janes.Count);
        }

        [Fact]
        public async Task RevokeSession_EndsIt()
        {
            var jane = await RegisterAsync("jane@example.com");
            var session = Assert.Single(await _t.Admin.ListSessionsAsync(_app.Id, null));

            await _t.Admin.RevokeSessionAsync(_app.Id, session.TokenId);

            Assert.Empty(await _t.Admin.ListSessionsAsync(_app.Id, null));
            await Assert.ThrowsAsync<InvalidRefreshTokenException>(() => _t.Auth.RefreshTokenAsync(jane.RefreshToken, "127.0.0.1", "test"));
        }

        [Fact]
        public async Task RevokeSession_OfAnotherApp_IsNotFound()
        {
            await RegisterAsync("jane@example.com");
            var session = Assert.Single(await _t.Admin.ListSessionsAsync(_app.Id, null));
            var other = await _t.CreateAppAsync("Other");

            await Assert.ThrowsAsync<SessionNotFoundException>(() => _t.Admin.RevokeSessionAsync(other.Id, session.TokenId));
        }
    }
}
