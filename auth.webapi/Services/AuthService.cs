using auth.webapi.DTO.Auth.Login;
using auth.webapi.DTO.Auth.Register;
using auth.webapi.Helpers;
using auth.webapi.Interfaces;
using auth.webapi.Models;
using Microsoft.AspNetCore.Http.HttpResults;
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

        public AuthService(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ILogger<AuthService> logger, ITokenService tokenService, IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _tokenService = tokenService;
            _emailService = emailService;
        }

        public async Task<ResponseUserDto> LoginUserAsync(LoginUserDto loginDto)
        {

            _logger.LogInformation("Starting");
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Email == loginDto.Email.ToLower());

            if (user == null)
                throw new InvalidCredentialsException();

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            if (!result.Succeeded)
                throw new InvalidCredentialsException();

            var refreshToken = _tokenService.CreateRefreshToken();
            var accessToken = _tokenService.CreateToken(user);

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);

            return new ResponseUserDto
            {
                Id = user.Id,
                FullName = user.FullName!,
                Email = user.Email!,
                UserName = user.UserName!,
                City = user.City!,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                RefreshTokenExpiryTime = user.RefreshTokenExpiryTime
            };
        }

        public async Task<ResponseUserDto> RegisterUserAsync(CreateUserDto createUserDto)
        {
            var appUser = new AppUser
            {
                Email = createUserDto.Email,
                City = createUserDto.City,
                FullName = createUserDto.FullName,
                UserName = createUserDto.Email.ToLower(),
            };

            _logger.LogInformation("user creating");
            var createdUser = await _userManager.CreateAsync(appUser, createUserDto.Password);
            if (!createdUser.Succeeded)
                throw new UserCreationFailedException("Failed to create the user");

            var roleResult = await _userManager.AddToRoleAsync(appUser, "User");
            if (!roleResult.Succeeded)
                throw new UserCreationFailedException("Failed to create the user (role issue)");

            _logger.LogInformation("creating tokens");
            var accessToken = _tokenService.CreateToken(appUser);
            var refreshToken = _tokenService.CreateRefreshToken();

            appUser.RefreshToken = refreshToken;
            appUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(30);
            await _userManager.UpdateAsync(appUser);

            _logger.LogInformation("sending email");
            await _emailService.SendEmailAsync(appUser.Email, "Welcome to Our App", $"<h1>Hello {appUser.UserName}!</h1><p>Thanks for registering!</p>");

            return new ResponseUserDto
            {
                Id = appUser.Id,
                FullName = appUser.FullName,
                Email = appUser.Email,
                City = appUser.City,
                UserName = appUser.UserName,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }
    }
}
