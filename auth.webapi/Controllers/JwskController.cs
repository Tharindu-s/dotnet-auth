using auth.webapi.Interfaces;
using auth.webapi.Services;
using Microsoft.AspNetCore.Mvc;

namespace auth.webapi.Controllers
{
    [ApiController]
    [Route(".well-known")]
    public class JwskController : ControllerBase
    {
        private readonly ITokenService _tokenService;

        public JwskController(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }

        [HttpGet("jwks.json")]
        public IActionResult GetJwks()
        {
            return new JsonResult(_tokenService.GetJwks());
        }

    }
}