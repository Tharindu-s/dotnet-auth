using System.Security.Claims;
using auth.webapi.DTO.Auth;
using auth.webapi.DTO.Auth.Login;
using auth.webapi.DTO.Auth.Register;
using auth.webapi.DTO.Auth.Token;
using auth.webapi.Helpers;
using auth.webapi.Interfaces;
using auth.webapi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace auth.webapi.Controllers
{
    [Route("api/auth")]
    [ApiController] // no need to validate the model state in each endpoint after adding this
    public class AccountController : ControllerBase
    {
        private readonly ILogger<AccountController> _logger;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IAuthService _authService;

        public AccountController(ILogger<AccountController> logger, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IAuthService authService)
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CreateUserDto createUserDto)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = Request.Headers["User-Agent"].ToString() ?? "Unknown";
            var apiKey = Request.Headers["X-Api-Key"].ToString();

            // converts the app id from the header to a Guid, if it fails it will return Guid.Empty
            Guid.TryParse(Request.Headers["X-App-Id"], out var appId);
            if (string.IsNullOrEmpty(apiKey) || appId == Guid.Empty)
            {
                throw new ApplicationClientAuthenticationException();
            }

            _logger.LogInformation("AppId is {AppId} and ApiKey is {ApiKey}", appId, apiKey);


            var user = await _authService.RegisterUserAsync(createUserDto, ipAddress, userAgent, appId, apiKey);

            Response.Cookies.Append("refreshToken", user.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = user.RefreshTokenExpiry
            });

            return Ok(new AuthenticatedUser
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                UserName = user.UserName,
                City = user.City,
                AccessToken = user.AccessToken
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserDto loginDto)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = Request.Headers["User-Agent"].ToString() ?? "Unknown";
            var apiKey = Request.Headers["X-Api-Key"].ToString();

            // converts the app id from the header to a Guid, if it fails it will return Guid.Empty
            Guid.TryParse(Request.Headers["X-App-Id"], out var appId);
            if (string.IsNullOrEmpty(apiKey) || appId == Guid.Empty)
            {
                throw new ApplicationClientAuthenticationException();
            }

            _logger.LogInformation("AppId is {AppId} and ApiKey is {ApiKey}", appId, apiKey);

            var user = await _authService.LoginUserAsync(loginDto, ipAddress, userAgent, appId, apiKey);

            Response.Cookies.Append("refreshToken", user.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = user.RefreshTokenExpiry
            });

            return Ok(new AuthenticatedUser
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                UserName = user.UserName,
                City = user.City,
                AccessToken = user.AccessToken
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = Request.Headers["User-Agent"].ToString() ?? "Unknown";

            if (refreshToken == null)
                throw new RefreshTokenExpiredException();


            var tokens = await _authService.RefreshTokenAsync(refreshToken, ipAddress, userAgent);

            Response.Cookies.Append("refreshToken", tokens.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return Ok(new { accessToken = tokens.AccessToken });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            var apiKey = Request.Headers["X-Api-Key"].ToString();

            // converts the app id from the header to a Guid, if it fails it will return Guid.Empty
            Guid.TryParse(Request.Headers["X-App-Id"], out var appId);
            if (string.IsNullOrEmpty(apiKey) || appId == Guid.Empty)
            {
                throw new ApplicationClientAuthenticationException();
            }

            _logger.LogInformation("AppId is {AppId} and ApiKey is {ApiKey}", appId, apiKey);

            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized("Refresh token not found");

            // gets user details from the claims(user doesn't have to send anything cause user claims are stored in access token that gets send through the api header). Sends the claim to the service for logic.
            await _authService.LogoutAsync(User, refreshToken, appId, apiKey);
            return Ok("Logged out successfully");
        }

        [Authorize]
        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions()
        {
            Console.WriteLine($"claim types are the following: {ClaimTypes.NameIdentifier}");
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var sessions = await _authService.GetUserSessionsAsync(userId);
            return Ok(sessions);
        }

        [HttpGet("test")]
        public async Task<IActionResult> Test()
        {
            return Ok(new { message = "Test successful" });
        }
    }
}