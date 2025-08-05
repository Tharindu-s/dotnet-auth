// using auth.webapi.Data;
// using auth.webapi.DTO.Auth.Login;
// using auth.webapi.Helpers;
// using auth.webapi.Interfaces;
// using auth.webapi.Models;
// using auth.webapi.Services;
// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Identity;
// using Microsoft.Extensions.Logging;
// using MockQueryable.Moq;
// using Moq;

// namespace auth.unitTest.Auth
// {
//     public class AuthServiceTests
//     {

//         private readonly Mock<UserManager<AppUser>> _userManagerMock;
//         private readonly Mock<SignInManager<AppUser>> _signInManagerMock;
//         private readonly Mock<ILogger<AuthService>> _loggerMock;
//         private readonly Mock<ITokenService> _tokenServiceMock;
//         private readonly Mock<IEmailService> _emailServiceMock;
//         private readonly Mock<IApplicationClientService> _applicationClientServiceMock;
//         private readonly Mock<ApplicationDbContext> _contextMock;
//         private readonly AuthService _authService;

//         public AuthServiceTests()
//         {
//             var store = new Mock<IUserStore<AppUser>>();
//             _userManagerMock = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);

//             var contextAccessor = new Mock<IHttpContextAccessor>();
//             var claimsFactory = new Mock<IUserClaimsPrincipalFactory<AppUser>>();
//             _signInManagerMock = new Mock<SignInManager<AppUser>>(
//                 _userManagerMock.Object,
//                 contextAccessor.Object,
//                 claimsFactory.Object,
//                 null,
//                 null,
//                 null,
//                 null
//             );
//             _loggerMock = new Mock<ILogger<AuthService>>();
//             _tokenServiceMock = new Mock<ITokenService>();
//             _emailServiceMock = new Mock<IEmailService>();
//             _applicationClientServiceMock = new Mock<IApplicationClientService>();
//             _contextMock = new Mock<ApplicationDbContext>();

//             _authService = new AuthService(
//                 _userManagerMock.Object,
//                 _signInManagerMock.Object,
//                 _loggerMock.Object,
//                 _tokenServiceMock.Object,
//                 _emailServiceMock.Object,
//                 _applicationClientServiceMock.Object,
//                 _contextMock.Object

//             );
//         }

//         [Fact]
//         public async Task LoginUserAsync_WithValidCredentials_ReturnsResponseUserDto()
//         {
//             // Arrange
//             var email = "test@example.com";
//             var password = "Password123!";
//             var ipAddress = "192.168.1.1";
//             var userAgent = "TestAgent";
//             var appId = Guid.NewGuid();
//             var apiKey = "test-api-key";
//             var hashedApiKey = "hashed-api-key";
//             var appUser = new AppUser
//             {
//                 Id = Guid.NewGuid().ToString(),
//                 Email = email,
//                 UserName = email,
//                 FullName = "Test User",
//                 City = "Test City",
//                 ApplicationClientId = appId
//             };

//             var appClient = new ApplicationClient
//             {
//                 Id = appId,
//                 ApiKeyHash = hashedApiKey
//             };

//             var mockUsers = new List<AppUser> { appUser }.AsQueryable().BuildMockDbSet();
//             var mockAppClients = new List<ApplicationClient> { appClient }.AsQueryable().BuildMockDbSet();
//             var mockRefreshTokens = new List<RefreshToken>().AsQueryable().BuildMockDbSet();

//             _userManagerMock.Setup(um => um.Users).Returns(mockUsers.Object);
//             _contextMock.Setup(c => c.ApplicationClient).Returns(mockAppClients.Object);
//             _contextMock.Setup(c => c.RefreshTokens).Returns(mockRefreshTokens.Object);

//             _applicationClientServiceMock.Setup(acs => acs.HashApiKey(apiKey)).Returns(hashedApiKey);

//             _signInManagerMock
//                 .Setup(sm => sm.CheckPasswordSignInAsync(appUser, password, false))
//                 .ReturnsAsync(SignInResult.Success);

//             _tokenServiceMock.Setup(ts => ts.CreateToken(It.IsAny<AppUser>())).Returns("mock_access_token");
//             _tokenServiceMock.Setup(ts => ts.CreateRefreshToken()).Returns("mock_refresh_token");

//             _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

//             var loginDto = new LoginUserDto
//             {
//                 Email = email,
//                 Password = password
//             };

//             // Act
//             var result = await _authService.LoginUserAsync(loginDto, ipAddress, userAgent, appId, apiKey);

//             // Assert
//             Assert.NotNull(result);
//             Assert.Equal("mock_access_token", result.AccessToken);
//             Assert.Equal("mock_refresh_token", result.RefreshToken);
//             Assert.Equal(email, result.Email);
//             Assert.Equal("Test User", result.FullName);
//             Assert.Equal("Test City", result.City);

//             // Verify that a refresh token was added to the context
//             _contextMock.Verify(c => c.RefreshTokens.Add(It.IsAny<RefreshToken>()), Times.Once);
//             _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
//         }

//         [Fact]
//         public async Task LoginUserAsync_WithInvalidCredentials_ThrowsException()
//         {
//             // Arrange
//             var email = "test@example.com";
//             var password = "wrongPassword";
//             var ipAddress = "192.168.1.1";
//             var userAgent = "TestAgent";
//             var appId = Guid.NewGuid();
//             var apiKey = "test-api-key";
//             var hashedApiKey = "hashed-api-key";

//             var existingUser = new AppUser
//             {
//                 Email = email,
//                 UserName = email,
//                 FullName = "Test User",
//                 City = "Test City",
//                 ApplicationClientId = appId
//             };

//             var appClient = new ApplicationClient
//             {
//                 Id = appId,
//                 ApiKeyHash = hashedApiKey
//             };

//             var mockUsers = new List<AppUser> { existingUser }.AsQueryable().BuildMockDbSet();
//             var mockAppClients = new List<ApplicationClient> { appClient }.AsQueryable().BuildMockDbSet();

//             _userManagerMock.Setup(x => x.Users).Returns(mockUsers.Object);
//             _contextMock.Setup(c => c.ApplicationClient).Returns(mockAppClients.Object);
//             _applicationClientServiceMock.Setup(acs => acs.HashApiKey(apiKey)).Returns(hashedApiKey);

//             _signInManagerMock
//                 .Setup(x => x.CheckPasswordSignInAsync(existingUser, password, false))
//                 .ReturnsAsync(SignInResult.Failed);

//             var loginDto = new LoginUserDto
//             {
//                 Email = email,
//                 Password = password
//             };

//             // Act & Assert
//             await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
//                 _authService.LoginUserAsync(loginDto, ipAddress, userAgent, appId, apiKey));
//         }
//     }
// }