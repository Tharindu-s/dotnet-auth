using System.Security.Claims;
using auth.webapi.DTO.Auth.Login;
using auth.webapi.DTO.Auth.Register;
using auth.webapi.DTO.Auth.Token;
using auth.webapi.Helpers;
using auth.webapi.Interfaces;
using auth.webapi.Models;
using auth.webapi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;

namespace auth.unitTest.Auth
{
    public class AuthServiceTests
    {

        private readonly Mock<UserManager<AppUser>> _userManagerMock;
        private readonly Mock<SignInManager<AppUser>> _signInManagerMock;
        private readonly Mock<ILogger<AuthService>> _loggerMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            var store = new Mock<IUserStore<AppUser>>();
            _userManagerMock = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);

            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<AppUser>>();
            _signInManagerMock = new Mock<SignInManager<AppUser>>(
                _userManagerMock.Object,
                contextAccessor.Object,
                claimsFactory.Object,
                null,
                null,
                null,
                null
            );

            _loggerMock = new Mock<ILogger<AuthService>>();
            _tokenServiceMock = new Mock<ITokenService>();
            _emailServiceMock = new Mock<IEmailService>();

            _authService = new AuthService(
                _userManagerMock.Object,
                _signInManagerMock.Object,
                _loggerMock.Object,
                _tokenServiceMock.Object,
                _emailServiceMock.Object
            );
        }

        [Fact]
        public async Task LoginUserAsync_WithValidCredentials_ReturnsResponseUserDto()
        {
            // Arrange
            var email = "test@example.com";
            var password = "Password123!";
            var appUser = new AppUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                UserName = email,
                FullName = "Test User",
                City = "Test City"
            };

            var mockUsers = new List<AppUser> { appUser }.AsQueryable().BuildMockDbSet();
            _userManagerMock.Setup(um => um.Users).Returns(mockUsers.Object);

            _signInManagerMock
                .Setup(sm => sm.CheckPasswordSignInAsync(appUser, password, false))
                .ReturnsAsync(SignInResult.Success);

            _tokenServiceMock.Setup(ts => ts.CreateToken(It.IsAny<AppUser>())).Returns("mock_access_token");
            _tokenServiceMock.Setup(ts => ts.CreateRefreshToken()).Returns("mock_refresh_token");

            // Act
            var result = await _authService.LoginUserAsync(new LoginUserDto
            {
                Email = email,
                Password = password
            });

            // Assert
            Assert.NotNull(result);
            Assert.Equal("mock_access_token", result.AccessToken);
            Assert.Equal("mock_refresh_token", result.RefreshToken);
            Assert.Equal(email, result.Email);
        }

        [Fact]
        public async Task LoginUserAsync_WithInvalidCredentials_ThrowsException()
        {
            // Arrange
            var email = "test@example.com";
            var password = "wrongPassword";

            var existingUser = new AppUser
            {
                Email = email,
                UserName = email,
                FullName = "Test User",
                City = "Test City"
            };

            // Mock a user found
            var mockUsers = new List<AppUser> { existingUser }.AsQueryable().BuildMockDbSet();
            _userManagerMock.Setup(x => x.Users).Returns(mockUsers.Object);

            // But password check fails
            _signInManagerMock
                .Setup(x => x.CheckPasswordSignInAsync(existingUser, password, false))
                .ReturnsAsync(SignInResult.Failed);

            var loginDto = new LoginUserDto
            {
                Email = email,
                Password = password
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
                _authService.LoginUserAsync(loginDto));
        }

        [Fact]
        public async Task LogoutAsync_WithValidEmail_LogsOutUser()
        {
            // Arrange
            var email = "user@example.com";

            var userPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
        new Claim(ClaimTypes.Email, email)
    }));

            var existingUser = new AppUser
            {
                Email = email,
                UserName = email,
                RefreshToken = "some-token",
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
            };

            var mockUsers = new List<AppUser> { existingUser }.AsQueryable().BuildMockDbSet();
            _userManagerMock.Setup(um => um.Users).Returns(mockUsers.Object);

            _userManagerMock.Setup(um => um.UpdateAsync(existingUser))
                            .ReturnsAsync(IdentityResult.Success);

            // Act
            await _authService.LogoutAsync(userPrincipal);

            // Assert
            Assert.Null(existingUser.RefreshToken);
            Assert.Equal(DateTime.MinValue, existingUser.RefreshTokenExpiryTime);
        }

        [Fact]
        public async Task LogoutAsync_WithMissingEmailClaim_ThrowsInvalidTokenException()
        {
            // Arrange
            var userPrincipal = new ClaimsPrincipal(new ClaimsIdentity()); // No claims

            // Act & Assert
            await Assert.ThrowsAsync<InvalidTokenException>(() =>
                _authService.LogoutAsync(userPrincipal));
        }
        [Fact]
        public async Task LogoutAsync_WithNonexistentUser_ThrowsUserNotFoundException()
        {
            // Arrange
            var email = "nonexistent@example.com";

            var userPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
        new Claim(ClaimTypes.Email, email)
    }));

            // No users returned
            var mockUsers = new List<AppUser>().AsQueryable().BuildMockDbSet();
            _userManagerMock.Setup(um => um.Users).Returns(mockUsers.Object);

            // Act & Assert
            await Assert.ThrowsAsync<UserNotFoundException>(() =>
                _authService.LogoutAsync(userPrincipal));
        }

        [Fact]
        public async Task RefreshTokenAsync_WithValidRequest_ReturnsNewTokens()
        {
            // Arrange
            var accessToken = "expiredAccessToken";
            var refreshToken = "validRefreshToken";
            var username = "testuser";

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
        new Claim(ClaimTypes.Name, username)
            }));

            _tokenServiceMock.Setup(t => t.GetPrincipalFromExpiredToken(accessToken))
                             .Returns(principal);

            var appUser = new AppUser
            {
                UserName = username,
                RefreshToken = refreshToken,
                RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(5)
            };

            var mockUsers = new List<AppUser> { appUser }.AsQueryable().BuildMockDbSet();
            _userManagerMock.Setup(u => u.Users).Returns(mockUsers.Object);

            _tokenServiceMock.Setup(t => t.CreateToken(appUser)).Returns("new-access-token");
            _tokenServiceMock.Setup(t => t.CreateRefreshToken()).Returns("new-refresh-token");

            _userManagerMock.Setup(u => u.UpdateAsync(appUser)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.RefreshTokenAsync(new SendTokenRefreshRequest
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });

            // Assert
            Assert.Equal("new-access-token", result.AccessToken);
            Assert.Equal("new-refresh-token", result.RefreshToken);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithInvalidAccessToken_ThrowsInvalidToken()
        {
            _tokenServiceMock.Setup(t => t.GetPrincipalFromExpiredToken(It.IsAny<string>()))
                             .Returns((ClaimsPrincipal)null); // Simulate invalid

            var dto = new SendTokenRefreshRequest
            {
                AccessToken = "invalid",
                RefreshToken = "irrelevant"
            };

            await Assert.ThrowsAsync<InvalidTokenException>(() =>
                _authService.RefreshTokenAsync(dto));
        }

        [Fact]
        public async Task RegisterUserAsync_WithValidData_ReturnsUserDto()
        {
            // Arrange
            var dto = new CreateUserDto
            {
                Email = "newuser@example.com",
                FullName = "New User",
                City = "City",
                Password = "StrongPass123!"
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync((AppUser)null); // Not found

            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<AppUser>(), dto.Password))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<AppUser>(), "User"))
                .ReturnsAsync(IdentityResult.Success);

            _tokenServiceMock.Setup(x => x.CreateToken(It.IsAny<AppUser>())).Returns("access-token");
            _tokenServiceMock.Setup(x => x.CreateRefreshToken()).Returns("refresh-token");

            _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<AppUser>())).ReturnsAsync(IdentityResult.Success);
            _emailServiceMock.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                             .Returns(Task.CompletedTask);

            // Act
            var result = await _authService.RegisterUserAsync(dto);

            // Assert
            Assert.Equal("newuser@example.com", result.Email);
            Assert.Equal("access-token", result.AccessToken);
            Assert.Equal("refresh-token", result.RefreshToken);
        }

    }
}