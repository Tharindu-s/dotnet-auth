using auth.webapi.Data;
using auth.webapi.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace auth.webapi.Controllers
{
    [Route("api/argus")]
    [ApiController]
    public class ArgusController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public ArgusController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Read by the Argus frontend at startup, so its app ID doesn't have to be baked into the build.
        // A public client's ID isn't a secret: it's sent from the browser on every sign-in anyway.
        [HttpGet("config")]
        public async Task<IActionResult> Config()
        {
            var appId = await _context.ApplicationClient
                .Where(a => a.IsSystem)
                .Select(a => (Guid?)a.Id)
                .FirstOrDefaultAsync()
                ?? throw new ApplicationClientNotFoundException();

            return Ok(new { appId, issuer = _configuration["JWT:Issuer"] });
        }
    }
}
