using auth.webapi.DTO.Auth.Register;
using auth.webapi.Interfaces;
using auth.webapi.Models;
using Microsoft.AspNetCore.Identity;

namespace auth.webapi.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;

        public AuthService(UserManager<AppUser> userManager, ITokenService tokenService, IEmailService emailService)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _emailService = emailService;
        }
        public async Task<(bool IsSuccess, ResponseUserDto? UserDto, IEnumerable<IdentityError>? Errors, string? ExceptionMessage)> RegisterUserAsync(CreateUserDto createUserDto)
        {
            try
            {
                var appUser = new AppUser
                {
                    Email = createUserDto.Email,
                    City = createUserDto.City,
                    FullName = createUserDto.FullName,
                    UserName = createUserDto.Email.ToLower(),
                };

                var createdUser = await _userManager.CreateAsync(appUser, createUserDto.Password);
                if (!createdUser.Succeeded)
                    return (false, null, createdUser.Errors, null);

                var roleResult = await _userManager.AddToRoleAsync(appUser, "User");
                if (!roleResult.Succeeded)
                    return (false, null, roleResult.Errors, null);

                var accessToken = _tokenService.CreateToken(appUser);
                var refreshToken = _tokenService.CreateRefreshToken();

                appUser.RefreshToken = refreshToken;
                appUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(30);
                await _userManager.UpdateAsync(appUser);

                await _emailService.SendEmailAsync(appUser.Email, "Welcome to Our App", $"<h1>Hello {appUser.UserName}!</h1><p>Thanks for registering!</p>");

                return (true, new ResponseUserDto
                {
                    Id = appUser.Id,
                    FullName = appUser.FullName,
                    Email = appUser.Email,
                    City = appUser.City,
                    UserName = appUser.UserName,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                }, null, null);
            }
            catch (Exception ex)
            {
                return (false, null, null, ex.Message);
            }
        }
    }
}
