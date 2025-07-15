using auth.webapi.DTO.Auth.Login;
using auth.webapi.DTO.Auth.Register;
using auth.webapi.DTO.Auth.Token;
using Microsoft.AspNetCore.Identity;

namespace auth.webapi.Interfaces
{
    public interface IAuthService
    {
        Task<ResponseUserDto> RegisterUserAsync(CreateUserDto createUserDto);
        Task<ResponseUserDto> LoginUserAsync(LoginUserDto loginDto);
        Task<ResponseTokenRefreshRequest> RefreshTokenAsync(SendTokenRefreshRequest refreshToken);
    }
}