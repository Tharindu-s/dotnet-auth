using auth.webapi.DTO.ApplicationClient;
using auth.webapi.Filters;
using auth.webapi.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace auth.webapi.Controllers
{
    [Route("api/application-client")]
    [ApiController] // no need to validate the model state in each endpoint after adding this
    [RequireAdminKey]
    public class ApplicationClientController : ControllerBase
    {
        private readonly IApplicationClientService _application;

        public ApplicationClientController(IApplicationClientService application)
        {
            _application = application;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAppId([FromBody] CreateApplicationClientDto dto)
        {
            var result = await _application.RegisterApplication(dto);
            return Ok(result);
        }
    }
}
