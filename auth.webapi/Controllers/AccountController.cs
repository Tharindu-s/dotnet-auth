using System.Security.Claims;
using auth.webapi.DTO.Auth.Login;
using auth.webapi.DTO.Auth.Register;
using auth.webapi.DTO.Auth.Token;
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
            var user = await _authService.RegisterUserAsync(createUserDto);
            return Ok(user);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserDto loginDto)
        {
            var user = await _authService.LoginUserAsync(loginDto);
            return Ok(user);
        }

        [Authorize]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] SendTokenRefreshRequest tokenDto)
        {
            var tokens = await _authService.RefreshTokenAsync(tokenDto);
            return Ok(tokens);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // gets user details from the claims(user doesn't have to send anything cause user claims are stored in access token that gets send through the api header). Sends the claim to the service for logic.
            await _authService.LogoutAsync(User);
            return Ok("Logged out successfully");
        }

        [Authorize]
        [HttpGet("test")]
        public async Task<IActionResult> Test()
        {

            return Ok("ok");
        }

    }
}