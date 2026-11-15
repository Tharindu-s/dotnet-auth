using auth.webapi.DTO.Auth;
using auth.webapi.DTO.Auth.Login;
using auth.webapi.DTO.Auth.Register;
using auth.webapi.DTO.Auth.Session;
using auth.webapi.DTO.Auth.Token;
using auth.webapi.Models;

namespace auth.webapi.Interfaces
{
    public interface IAuthService
    {
        Task<ResponseUserDto> RegisterUserAsync(CreateUserDto createUserDto, ApplicationClient app, string ipAddress, string userAgent);
        Task<ResponseUserDto> LoginUserAsync(LoginUserDto loginDto, ApplicationClient app, string ipAddress, string userAgent);
        Task<ResponseTokenRefreshRequest> RefreshTokenAsync(string refreshToken, string ipAddress, string userAgent);
        Task LogoutAsync(string userId, string? refreshToken, ApplicationClient app);
        Task<CurrentUserDto> GetCurrentUserAsync(string userId);
        Task<List<UserSessionDto>> GetUserSessionsAsync(string userId, string? currentRefreshToken);
        Task RevokeSessionAsync(string userId, int sessionId);

        // Also clears MustChangePassword and revokes every session except the one currentRefreshToken belongs to
        Task ChangePasswordAsync(string userId, ChangePasswordDto dto, string? currentRefreshToken);
    }
}
