using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using auth.webapi.DTO.ApplicationClient;
using auth.webapi.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace auth.webapi.Controllers
{
    [Route("api/application-client")]
    [ApiController] // no need to validate the model state in each endpoint after adding this
    public class ApplicationClientController : ControllerBase
    {
        private readonly ILogger<ApplicationClientController> _logger;
        private readonly IApplicationClientService _application;

        public ApplicationClientController(ILogger<ApplicationClientController> logger, IApplicationClientService application)
        {
            _logger = logger;
            _application = application;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAppId([FromBody] CreateApplicationClientDto dto)
        {
            var result = await _application.RegisterApplication(dto);

            return Ok(new ResponseCreateApplicationClientDto
            {
                AppId = result.AppId,
                RawApiKey = result.RawApiKey
            });
        }


    }
}