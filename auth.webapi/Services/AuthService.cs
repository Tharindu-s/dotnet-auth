using System.Security.Claims;
using auth.webapi.Data;
using auth.webapi.DTO.Auth;
using auth.webapi.DTO.Auth.Login;
using auth.webapi.DTO.Auth.Register;
using auth.webapi.DTO.Auth.Session;
using auth.webapi.DTO.Auth.Token;
using auth.webapi.Helpers;
using auth.webapi.Interfaces;
using auth.webapi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace auth.webapi.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ILogger<AuthService> _logger;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IApplicationClientService _applicationClientService;
        private readonly ApplicationDbContext _context;

        public AuthService(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ILogger<AuthService> logger, ITokenService tokenService, IEmailService emailService, IApplicationClientService applicationClientService, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _tokenService = tokenService;
            _emailService = emailService;
            _applicationClientService = applicationClientService;
            _context = context;
        }

        public async Task<List<UserSessionDto>> GetUserSessionsAsync(string userId)
        {
            var sessions = await _context.RefreshTokens
        .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.Expires > DateTime.UtcNow)
        .Select(rt => new UserSessionDto
        {
            TokenId = rt.Id,
            Device = rt.Device,
            IPAddress = rt.IPAddress,
            ExpiresAt = rt.Expires
        })
        .ToListAsync();

            return sessions;
        }

        public async Task<ResponseUserDto> LoginUserAsync(LoginUserDto loginDto, string ipAddress, string userAgent, Guid appId, string apiKey)
        {

            var hashedApiKey = _applicationClientService.HashApiKey(apiKey);
            _logger.LogInformation("hashed api key is {HashedApiKey}", hashedApiKey);
            // Validate the client application using the provided appId and apiKey
            var appClient = await _context.ApplicationClient.FirstOrDefaultAsync(a => a.Id == appId && a.ApiKeyHash == hashedApiKey);

            if (appClient == null)
                throw new ApplicationClientAuthenticationException("Client application credentials do not match");

            _logger.LogInformation("Starting");
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Email == loginDto.Email.ToLower() && x.ApplicationClientId == appId);

            if (user == null)
                throw new InvalidCredentialsException();

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            if (!result.Succeeded)
                throw new InvalidCredentialsException();

            var refreshToken = _tokenService.CreateRefreshToken();
            var accessToken = _tokenService.CreateToken(user);


            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshToken,
                Expires = DateTime.UtcNow.AddDays(7),
                UserId = user.Id,
                Device = userAgent,
                IPAddress = ipAddress,
                ApplicationClientId = appId
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            return new ResponseUserDto
            {
                Id = user.Id,
                FullName = user.FullName!,
                Email = user.Email!,
                UserName = user.UserName!,
                City = user.City!,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                RefreshTokenExpiry = refreshTokenEntity.Expires
            };
        }

        public async Task LogoutAsync(ClaimsPrincipal userPrincipal, string refreshToken, Guid appId, string apiKey)
        {
            var validEmail = userPrincipal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (validEmail == null)
                throw new InvalidTokenException();
            // cause the details are taken from the token

            var user = await _userManager.Users.FirstOrDefaultAsync(e => e.Email == validEmail && e.ApplicationClientId == appId);

            if (user == null)
                throw new UserNotFoundException();

            var currentToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.UserId == user.Id && rt.Token == refreshToken && !rt.IsRevoked);

            if (currentToken == null)
                throw new InvalidTokenException();

            // revoke the token
            currentToken.IsRevoked = true;
            await _context.SaveChangesAsync();

            Console.WriteLine("logged out successfully");
        }

        public async Task<ResponseTokenRefreshRequest> RefreshTokenAsync(string refreshToken, string ipAddress, string userAgent)
        {
            // Find the refresh token in DB
            var currentToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked);

            if (currentToken == null || currentToken.Expires <= DateTime.UtcNow)
                throw new RefreshTokenExpiredException();

            var user = currentToken.User;
            if (user == null)
                throw new UserNotFoundException();

            // Generate new tokens
            var newAccessToken = _tokenService.CreateToken(user);
            var newRefreshToken = _tokenService.CreateRefreshToken();

            // Revoke the old token and save the new one
            currentToken.IsRevoked = true;

            var refreshTokenEntity = new RefreshToken
            {
                Token = newRefreshToken,
                UserId = user.Id,
                Expires = DateTime.UtcNow.AddDays(7),
                Device = userAgent,
                IPAddress = ipAddress,
                ApplicationClientId = currentToken.ApplicationClientId
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            return new ResponseTokenRefreshRequest
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }
        public async Task<ResponseUserDto> RegisterUserAsync(CreateUserDto createUserDto, string ipAddress, string userAgent, Guid appId, string apiKey)
        {
            var hashedApiKey = _applicationClientService.HashApiKey(apiKey);
            _logger.LogInformation("hashed api key is {HashedApiKey}", hashedApiKey);
            // Validate the client application using the provided appId and apiKey
            var appClient = await _context.ApplicationClient.FirstOrDefaultAsync(a => a.Id == appId && a.ApiKeyHash == hashedApiKey);

            if (appClient == null)
                throw new ApplicationClientAuthenticationException("Client application credentials do not match");

            var appUser = new AppUser
            {
                Email = createUserDto.Email,
                City = createUserDto.City,
                FullName = createUserDto.FullName,
                UserName = createUserDto.Email.ToLower(),
                ApplicationClientId = appClient.Id,
            };

            var existingUser = await _context.Users.Where(u => u.Email == createUserDto.Email && u.ApplicationClientId == appId).FirstOrDefaultAsync();

            if (existingUser != null)
                throw new EmailAlreadyExistsException("User with this email already exists");

            var createdUser = await _userManager.CreateAsync(appUser, createUserDto.Password);
            if (!createdUser.Succeeded)
                throw new UserCreationFailedException("Failed to create the user");

            var roleResult = await _userManager.AddToRoleAsync(appUser, "User");
            if (!roleResult.Succeeded)
                throw new UserCreationFailedException("Failed to create the user (role issue)");

            var accessToken = _tokenService.CreateToken(appUser);
            var refreshToken = _tokenService.CreateRefreshToken();

            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshToken,
                Expires = DateTime.UtcNow.AddDays(7),
                UserId = appUser.Id,
                Device = userAgent,
                IPAddress = ipAddress,
                ApplicationClientId = appId
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            await _emailService.SendEmailAsync(appUser.Email, "Welcome to Our App", $"<h1>Hello {appUser.UserName}!</h1><p>Thanks for registering!</p>");

            return new ResponseUserDto
            {
                Id = appUser.Id,
                FullName = appUser.FullName!,
                Email = appUser.Email!,
                UserName = appUser.UserName!,
                City = appUser.City!,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                RefreshTokenExpiry = refreshTokenEntity.Expires
            };
        }
    }
}
