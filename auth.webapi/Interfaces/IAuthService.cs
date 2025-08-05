using System.Security.Claims;
using auth.webapi.DTO.Auth;
using auth.webapi.DTO.Auth.Login;
using auth.webapi.DTO.Auth.Register;
using auth.webapi.DTO.Auth.Session;
using auth.webapi.DTO.Auth.Token;

namespace auth.webapi.Interfaces
{
    public interface IAuthService
    {
        Task<ResponseUserDto> RegisterUserAsync(CreateUserDto createUserDto, string ipAddress, string userAgent);
        Task<ResponseUserDto> LoginUserAsync(LoginUserDto loginDto, string ipAddress, string userAgent);
        Task<ResponseTokenRefreshRequest> RefreshTokenAsync(string accessToken, string refreshToken, string ipAddress, string userAgent);
        Task LogoutAsync(ClaimsPrincipal userPrincipal, string refreshToken);
        Task<List<UserSessionDto>> GetUserSessionsAsync(string userId);
    }
}