using auth.webapi.DTO.Auth.Login;
using auth.webapi.DTO.Auth.Register;
using auth.webapi.Helpers;
using auth.webapi.Models;
using auth.webapi.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace auth.unitTest.Auth
{
    public class AuthServiceTests : IAsyncLifetime
    {
        private const string Password = "Passw0rd!";
        private const string Ip = "127.0.0.1";
        private const string Agent = "unit-test";

        private TestServices _t = null!;
        private ApplicationClient _app = null!;

        public async Task InitializeAsync()
        {
            _t = await TestServices.CreateAsync();
            _app = await _t.CreateAppAsync();
        }

        public async Task DisposeAsync() => await _t.DisposeAsync();

        private static CreateUserDto NewUser(string email = "jane@example.com") => new()
        {
            FullName = "Jane Doe",
            Email = email,
            City = "Colombo",
            Password = Password
        };

        private Task<auth.webapi.DTO.Auth.ResponseUserDto> RegisterAsync(string email = "jane@example.com", ApplicationClient? app = null) =>
            _t.Auth.RegisterUserAsync(NewUser(email), app ?? _app, Ip, Agent);

        private Task<auth.webapi.DTO.Auth.ResponseUserDto> LoginAsync(string email, string password = Password, ApplicationClient? app = null) =>
            _t.Auth.LoginUserAsync(new LoginUserDto { Email = email, Password = password }, app ?? _app, Ip, Agent);

        private Task<RefreshToken> FindTokenAsync(string rawToken)
        {
            var hash = _t.Tokens.HashRefreshToken(rawToken);
            return _t.Db.RefreshTokens.AsNoTracking().SingleAsync(rt => rt.TokenHash == hash);
        }

        // Register

        [Fact]
        public async Task Register_StoresOnlyTheRefreshTokenHash()
        {
            var user = await RegisterAsync();

            var stored = await FindTokenAsync(user.RefreshToken);
            Assert.NotEqual(user.RefreshToken, stored.TokenHash);
            Assert.False(await _t.Db.RefreshTokens.AnyAsync(rt => rt.TokenHash == user.RefreshToken));
            Assert.NotEqual(default, stored.Created);
        }

        [Fact]
        public async Task Register_SendsWelcomeEmail()
        {
            await RegisterAsync();

            _t.Email.Verify(e => e.SendEmailAsync("jane@example.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Register_DuplicateEmailInSameApp_DifferentCase_Throws()
        {
            await RegisterAsync("jane@example.com");

            await Assert.ThrowsAsync<EmailAlreadyExistsException>(() => RegisterAsync("JANE@Example.com"));
        }

        [Fact]
        public async Task Register_SameEmailInDifferentApps_Succeeds()
        {
            var otherApp = await _t.CreateAppAsync("Other App");

            var first = await RegisterAsync(app: _app);
            var second = await RegisterAsync(app: otherApp);

            Assert.NotEqual(first.Id, second.Id);
        }

        [Fact]
        public async Task Register_WeakPassword_ThrowsWithIdentityErrors()
        {
            var dto = NewUser();
            dto.Password = "short";

            var ex = await Assert.ThrowsAsync<UserCreationFailedException>(() => _t.Auth.RegisterUserAsync(dto, _app, Ip, Agent));
            Assert.Contains("at least 6 characters", ex.Message);
        }

        // Login

        [Fact]
        public async Task Login_MixedCaseEmail_Succeeds()
        {
            await RegisterAsync("Jane.Doe@Example.com");

            var user = await LoginAsync("jane.doe@EXAMPLE.com");

            Assert.Equal("Jane.Doe@Example.com", user.Email);
        }

        [Fact]
        public async Task Login_WrongPassword_Throws()
        {
            await RegisterAsync();

            await Assert.ThrowsAsync<InvalidCredentialsException>(() => LoginAsync("jane@example.com", "Wr0ng!pass"));
        }

        [Fact]
        public async Task Login_UserFromAnotherApp_Throws()
        {
            var otherApp = await _t.CreateAppAsync("Other App");
            await RegisterAsync(app: otherApp);

            await Assert.ThrowsAsync<InvalidCredentialsException>(() => LoginAsync("jane@example.com", app: _app));
        }

        [Fact]
        public async Task Login_LocksAccountAfterFiveFailures()
        {
            await RegisterAsync();

            for (var i = 0; i < 4; i++)
                await Assert.ThrowsAsync<InvalidCredentialsException>(() => LoginAsync("jane@example.com", "Wr0ng!pass"));

            await Assert.ThrowsAsync<AccountLockedException>(() => LoginAsync("jane@example.com", "Wr0ng!pass"));
            // Even the right password is rejected while locked
            await Assert.ThrowsAsync<AccountLockedException>(() => LoginAsync("jane@example.com"));
        }

        [Fact]
        public async Task Login_StartsANewTokenFamily()
        {
            var registered = await RegisterAsync();
            var loggedIn = await LoginAsync("jane@example.com");

            var a = await FindTokenAsync(registered.RefreshToken);
            var b = await FindTokenAsync(loggedIn.RefreshToken);
            Assert.NotEqual(a.FamilyId, b.FamilyId);
        }

        // Refresh

        [Fact]
        public async Task Refresh_RotatesTokenWithinFamily()
        {
            var user = await RegisterAsync();

            var refreshed = await _t.Auth.RefreshTokenAsync(user.RefreshToken, Ip, Agent);

            var old = await FindTokenAsync(user.RefreshToken);
            var current = await FindTokenAsync(refreshed.RefreshToken);
            Assert.True(old.IsRevoked);
            Assert.Equal(current.Id, old.ReplacedByTokenId);
            Assert.Equal(old.FamilyId, current.FamilyId);
            Assert.False(current.IsRevoked);
        }

        [Fact]
        public async Task Refresh_UnknownToken_Throws()
        {
            await Assert.ThrowsAsync<InvalidRefreshTokenException>(() => _t.Auth.RefreshTokenAsync("not-a-token", Ip, Agent));
        }

        [Fact]
        public async Task Refresh_ExpiredToken_Throws()
        {
            var user = await RegisterAsync();
            var hash = _t.Tokens.HashRefreshToken(user.RefreshToken);
            var aMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
            await _t.Db.RefreshTokens.Where(rt => rt.TokenHash == hash)
                .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.Expires, aMinuteAgo));

            await Assert.ThrowsAsync<RefreshTokenExpiredException>(() => _t.Auth.RefreshTokenAsync(user.RefreshToken, Ip, Agent));
        }

        [Fact]
        public async Task Refresh_ReuseWithinGracePeriod_FailsButKeepsFamily()
        {
            var user = await RegisterAsync();
            var refreshed = await _t.Auth.RefreshTokenAsync(user.RefreshToken, Ip, Agent);

            await Assert.ThrowsAsync<InvalidRefreshTokenException>(() => _t.Auth.RefreshTokenAsync(user.RefreshToken, Ip, Agent));

            // The legitimate holder of the new token is unaffected
            var current = await FindTokenAsync(refreshed.RefreshToken);
            Assert.False(current.IsRevoked);
        }

        [Fact]
        public async Task Refresh_ReuseAfterGracePeriod_RevokesWholeFamily()
        {
            var user = await RegisterAsync();
            var refreshed = await _t.Auth.RefreshTokenAsync(user.RefreshToken, Ip, Agent);

            var oldHash = _t.Tokens.HashRefreshToken(user.RefreshToken);
            var outsideGrace = DateTime.UtcNow - AuthService.ReuseGracePeriod - TimeSpan.FromSeconds(1);
            await _t.Db.RefreshTokens.Where(rt => rt.TokenHash == oldHash)
                .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.RevokedAt, outsideGrace));

            await Assert.ThrowsAsync<InvalidRefreshTokenException>(() => _t.Auth.RefreshTokenAsync(user.RefreshToken, Ip, Agent));

            var current = await FindTokenAsync(refreshed.RefreshToken);
            Assert.True(current.IsRevoked);
            await Assert.ThrowsAsync<InvalidRefreshTokenException>(() => _t.Auth.RefreshTokenAsync(refreshed.RefreshToken, Ip, Agent));
        }

        [Fact]
        public async Task Refresh_ReuseDetection_LeavesOtherSessionsAlone()
        {
            var first = await RegisterAsync();
            var otherDevice = await LoginAsync("jane@example.com");
            await _t.Auth.RefreshTokenAsync(first.RefreshToken, Ip, Agent);

            var oldHash = _t.Tokens.HashRefreshToken(first.RefreshToken);
            var anHourAgo = DateTime.UtcNow.AddHours(-1);
            await _t.Db.RefreshTokens.Where(rt => rt.TokenHash == oldHash)
                .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.RevokedAt, anHourAgo));
            await Assert.ThrowsAsync<InvalidRefreshTokenException>(() => _t.Auth.RefreshTokenAsync(first.RefreshToken, Ip, Agent));

            var other = await _t.Auth.RefreshTokenAsync(otherDevice.RefreshToken, Ip, Agent);
            Assert.NotNull(other.AccessToken);
        }

        // Logout and sessions

        [Fact]
        public async Task Logout_RevokesOnlyCurrentSession()
        {
            var first = await RegisterAsync();
            var second = await LoginAsync("jane@example.com");

            await _t.Auth.LogoutAsync(first.Id, first.RefreshToken, _app);

            Assert.True((await FindTokenAsync(first.RefreshToken)).IsRevoked);
            Assert.False((await FindTokenAsync(second.RefreshToken)).IsRevoked);
            await Assert.ThrowsAsync<InvalidRefreshTokenException>(() => _t.Auth.RefreshTokenAsync(first.RefreshToken, Ip, Agent));
        }

        [Fact]
        public async Task Logout_IsIdempotent()
        {
            var user = await RegisterAsync();

            await _t.Auth.LogoutAsync(user.Id, user.RefreshToken, _app);
            await _t.Auth.LogoutAsync(user.Id, user.RefreshToken, _app);
            await _t.Auth.LogoutAsync(user.Id, null, _app);
        }

        [Fact]
        public async Task Logout_WithAnotherAppsCredentials_DoesNotRevoke()
        {
            var otherApp = await _t.CreateAppAsync("Other App");
            var user = await RegisterAsync();

            await _t.Auth.LogoutAsync(user.Id, user.RefreshToken, otherApp);

            Assert.False((await FindTokenAsync(user.RefreshToken)).IsRevoked);
        }

        [Fact]
        public async Task Sessions_ListsActiveSessionsWithIssuedAt()
        {
            var first = await RegisterAsync();
            await LoginAsync("jane@example.com");
            await _t.Auth.LogoutAsync(first.Id, first.RefreshToken, _app);

            var sessions = await _t.Auth.GetUserSessionsAsync(first.Id, null);

            var session = Assert.Single(sessions);
            Assert.NotEqual(default, session.IssuedAt);
            Assert.Equal(Agent, session.Device);
            Assert.False(session.IsCurrent);
        }

        [Fact]
        public async Task Sessions_MarksTheCallersSessionAsCurrent()
        {
            var first = await RegisterAsync();
            var second = await LoginAsync("jane@example.com");

            var sessions = await _t.Auth.GetUserSessionsAsync(first.Id, second.RefreshToken);

            Assert.Equal(2, sessions.Count);
            var current = Assert.Single(sessions, s => s.IsCurrent);
            Assert.Equal((await FindTokenAsync(second.RefreshToken)).Id, current.TokenId);
        }

        [Fact]
        public async Task RevokeSession_EndsThatSessionOnly()
        {
            var first = await RegisterAsync();
            var second = await LoginAsync("jane@example.com");
            var firstId = (await FindTokenAsync(first.RefreshToken)).Id;

            await _t.Auth.RevokeSessionAsync(first.Id, firstId);

            await Assert.ThrowsAsync<InvalidRefreshTokenException>(() => _t.Auth.RefreshTokenAsync(first.RefreshToken, Ip, Agent));
            Assert.NotNull((await _t.Auth.RefreshTokenAsync(second.RefreshToken, Ip, Agent)).AccessToken);
        }

        [Fact]
        public async Task RevokeSession_OfAnotherUser_Throws()
        {
            var jane = await RegisterAsync("jane@example.com");
            var john = await RegisterAsync("john@example.com");
            var janesSession = (await FindTokenAsync(jane.RefreshToken)).Id;

            await Assert.ThrowsAsync<SessionNotFoundException>(() => _t.Auth.RevokeSessionAsync(john.Id, janesSession));
            Assert.False((await FindTokenAsync(jane.RefreshToken)).IsRevoked);
        }

        // Current user

        [Fact]
        public async Task GetCurrentUser_ReturnsProfileWithRolesAndApp()
        {
            var user = await RegisterAsync();

            var me = await _t.Auth.GetCurrentUserAsync(user.Id);

            Assert.Equal("jane@example.com", me.Email);
            Assert.Equal(_app.Id, me.AppId);
            Assert.Equal(new[] { "User" }, me.Roles);
        }

        [Fact]
        public async Task GetCurrentUser_Unknown_Throws()
        {
            await Assert.ThrowsAsync<UserNotFoundException>(() => _t.Auth.GetCurrentUserAsync("missing"));
        }

        // Registration switch

        [Fact]
        public async Task Register_WhenSignUpsAreOff_Throws()
        {
            var closed = await _t.CreateAppAsync("Closed", registrationEnabled: false);

            await Assert.ThrowsAsync<RegistrationDisabledException>(() => RegisterAsync(app: closed));
            Assert.False(await _t.Db.Users.AnyAsync());
        }

        // Change password

        [Fact]
        public async Task ChangePassword_NewPasswordWorks_OldDoesNot()
        {
            var user = await RegisterAsync();

            await _t.Auth.ChangePasswordAsync(user.Id, new() { CurrentPassword = Password, NewPassword = "N3w-Passw0rd!" }, user.RefreshToken);

            await LoginAsync("jane@example.com", "N3w-Passw0rd!");
            await Assert.ThrowsAsync<InvalidCredentialsException>(() => LoginAsync("jane@example.com", Password));
        }

        [Fact]
        public async Task ChangePassword_KeepsThisSession_RevokesTheOthers()
        {
            var thisDevice = await RegisterAsync();
            var otherDevice = await LoginAsync("jane@example.com");

            await _t.Auth.ChangePasswordAsync(thisDevice.Id, new() { CurrentPassword = Password, NewPassword = "N3w-Passw0rd!" }, thisDevice.RefreshToken);

            Assert.False((await FindTokenAsync(thisDevice.RefreshToken)).IsRevoked);
            Assert.True((await FindTokenAsync(otherDevice.RefreshToken)).IsRevoked);
        }

        [Fact]
        public async Task ChangePassword_ClearsMustChangePassword()
        {
            var user = await RegisterAsync();
            await _t.Db.Users.Where(u => u.Id == user.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.MustChangePassword, true));

            await _t.Auth.ChangePasswordAsync(user.Id, new() { CurrentPassword = Password, NewPassword = "N3w-Passw0rd!" }, null);

            Assert.False((await _t.Auth.GetCurrentUserAsync(user.Id)).MustChangePassword);
        }

        [Fact]
        public async Task ChangePassword_WrongCurrentPassword_ThrowsAndKeepsTheFlag()
        {
            var user = await RegisterAsync();
            await _t.Db.Users.Where(u => u.Id == user.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.MustChangePassword, true));

            var error = await Assert.ThrowsAsync<PasswordChangeFailedException>(() =>
                _t.Auth.ChangePasswordAsync(user.Id, new() { CurrentPassword = "Wr0ng-pass!", NewPassword = "N3w-Passw0rd!" }, null));

            Assert.Equal("The current password is incorrect.", error.Message);
            Assert.True((await _t.Auth.GetCurrentUserAsync(user.Id)).MustChangePassword);
        }

        [Theory]
        [InlineData("short")]
        [InlineData(Password)]
        public async Task ChangePassword_WeakOrUnchangedPassword_Throws(string newPassword)
        {
            var user = await RegisterAsync();

            await Assert.ThrowsAsync<PasswordChangeFailedException>(() =>
                _t.Auth.ChangePasswordAsync(user.Id, new() { CurrentPassword = Password, NewPassword = newPassword }, null));
        }
    }
}
