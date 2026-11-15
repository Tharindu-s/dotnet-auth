using System.Security.Claims;
using auth.webapi.DTO.Auth;
using auth.webapi.DTO.Auth.Login;
using auth.webapi.DTO.Auth.Register;
using auth.webapi.Extentions;
using auth.webapi.Filters;
using auth.webapi.Helpers;
using auth.webapi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace auth.webapi.Controllers
{
    [Route("api/auth")]
    [ApiController] // no need to validate the model state in each endpoint after adding this
    public class AccountController : ControllerBase
    {
        private const string RefreshTokenCookie = "refreshToken";

        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        private string IpAddress => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        private string UserAgent => Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : "Unknown";
        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidTokenException();

        [HttpPost("register")]
        [RequireApplicationClient]
        [EnableRateLimiting(ServiceExtentions.CredentialsRateLimitPolicy)]
        public async Task<IActionResult> Register([FromBody] CreateUserDto createUserDto)
        {
            var user = await _authService.RegisterUserAsync(createUserDto, HttpContext.GetApplicationClient(), IpAddress, UserAgent);
            SetRefreshTokenCookie(user.RefreshToken, user.RefreshTokenExpiry);
            return Ok(ToAuthenticatedUser(user));
        }

        [HttpPost("login")]
        [RequireApplicationClient]
        [EnableRateLimiting(ServiceExtentions.CredentialsRateLimitPolicy)]
        public async Task<IActionResult> Login([FromBody] LoginUserDto loginDto)
        {
            var user = await _authService.LoginUserAsync(loginDto, HttpContext.GetApplicationClient(), IpAddress, UserAgent);
            SetRefreshTokenCookie(user.RefreshToken, user.RefreshTokenExpiry);
            return Ok(ToAuthenticatedUser(user));
        }

        [HttpPost("refresh")]
        [EnableRateLimiting(ServiceExtentions.RefreshRateLimitPolicy)]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = Request.Cookies[RefreshTokenCookie];
            if (string.IsNullOrEmpty(refreshToken))
                throw new InvalidRefreshTokenException("Refresh token not found.");

            var tokens = await _authService.RefreshTokenAsync(refreshToken, IpAddress, UserAgent);
            SetRefreshTokenCookie(tokens.RefreshToken, tokens.RefreshTokenExpiry);
            return Ok(new { accessToken = tokens.AccessToken });
        }

        [Authorize]
        [HttpPost("logout")]
        [RequireApplicationClient]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync(CurrentUserId, Request.Cookies[RefreshTokenCookie], HttpContext.GetApplicationClient());
            DeleteRefreshTokenCookie();
            return NoContent();
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            return Ok(await _authService.GetCurrentUserAsync(CurrentUserId));
        }

        [Authorize]
        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions()
        {
            var sessions = await _authService.GetUserSessionsAsync(CurrentUserId, Request.Cookies[RefreshTokenCookie]);
            return Ok(sessions);
        }

        // Signs out another device. To end the current session, use logout so the cookie is cleared too.
        [Authorize]
        [HttpDelete("sessions/{id:int}")]
        public async Task<IActionResult> RevokeSession(int id)
        {
            await _authService.RevokeSessionAsync(CurrentUserId, id);
            return NoContent();
        }

        // Signs out the user's other devices; this session stays signed in
        [Authorize]
        [HttpPost("change-password")]
        [EnableRateLimiting(ServiceExtentions.CredentialsRateLimitPolicy)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            await _authService.ChangePasswordAsync(CurrentUserId, dto, Request.Cookies[RefreshTokenCookie]);
            return NoContent();
        }

        private static AuthenticatedUser ToAuthenticatedUser(ResponseUserDto user) => new()
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            UserName = user.UserName,
            City = user.City,
            AccessToken = user.AccessToken
        };

        private static CookieOptions RefreshCookieOptions(DateTime? expires = null) => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = expires
        };

        private void SetRefreshTokenCookie(string refreshToken, DateTime expires)
        {
            Response.Cookies.Append(RefreshTokenCookie, refreshToken, RefreshCookieOptions(expires));
        }

        private void DeleteRefreshTokenCookie()
        {
            Response.Cookies.Delete(RefreshTokenCookie, RefreshCookieOptions());
        }
    }
}
