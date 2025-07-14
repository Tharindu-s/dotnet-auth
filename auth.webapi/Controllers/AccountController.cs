using auth.webapi.DTO.Auth.Register;
using auth.webapi.Interfaces;
using auth.webapi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace auth.webapi.Controllers
{
    [Route("api/auth")]
    [ApiController]

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
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.RegisterUserAsync(createUserDto);

                // checks if an exception occurred during registration inside the service method, and if so, it returns an HTTP 400 Bad Request with the exception message
                if (!string.IsNullOrEmpty(result.ExceptionMessage))
                    return BadRequest(result.ExceptionMessage);

                if (!result.IsSuccess)
                {
                    return BadRequest(result.Errors);
                }

                return Ok(result.UserDto);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}