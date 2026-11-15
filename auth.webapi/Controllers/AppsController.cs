using auth.webapi.DTO.ApplicationClient;
using auth.webapi.Filters;
using auth.webapi.Interfaces;
using auth.webapi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace auth.webapi.Controllers
{
    // The admin console's API: manage application clients, their users and their sessions.
    // The Argus app itself is never returned and can't be changed here.
    [Route("api/apps")]
    [ApiController]
    [Authorize]
    [RequireArgusAdmin]
    public class AppsController : ControllerBase
    {
        private readonly IApplicationClientService _apps;
        private readonly IAppUserAdminService _users;

        public AppsController(IApplicationClientService apps, IAppUserAdminService users)
        {
            _apps = apps;
            _users = users;
        }

        [HttpGet]
        public async Task<IActionResult> List() => Ok(await _apps.ListAsync());

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id) => Ok(await _apps.GetAsync(id));

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateApplicationClientDto dto)
        {
            var created = await _apps.RegisterApplication(dto);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateApplicationClientDto dto) =>
            Ok(await _apps.UpdateAsync(id, dto));

        // The old key stops working immediately
        [HttpPost("{id:guid}/rotate-key")]
        public async Task<IActionResult> RotateKey(Guid id) =>
            Ok(new RotatedApiKeyDto { RawApiKey = await _apps.RotateApiKeyAsync(id) });

        // Also deletes the app's users and sessions
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _apps.DeleteAsync(id);
            return NoContent();
        }

        [HttpGet("{id:guid}/users")]
        public async Task<IActionResult> ListUsers(Guid id, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 25) =>
            Ok(await _users.ListUsersAsync(id, search, page, pageSize));

        [HttpPost("{id:guid}/users/{userId}/lock")]
        public async Task<IActionResult> LockUser(Guid id, string userId)
        {
            await _users.LockUserAsync(id, userId);
            return NoContent();
        }

        [HttpPost("{id:guid}/users/{userId}/unlock")]
        public async Task<IActionResult> UnlockUser(Guid id, string userId)
        {
            await _users.UnlockUserAsync(id, userId);
            return NoContent();
        }

        [HttpDelete("{id:guid}/users/{userId}")]
        public async Task<IActionResult> DeleteUser(Guid id, string userId)
        {
            await _users.DeleteUserAsync(id, userId);
            return NoContent();
        }

        [HttpGet("{id:guid}/sessions")]
        public async Task<IActionResult> ListSessions(Guid id, [FromQuery] string? userId) =>
            Ok(await _users.ListSessionsAsync(id, userId));

        [HttpDelete("{id:guid}/sessions/{sessionId:int}")]
        public async Task<IActionResult> RevokeSession(Guid id, int sessionId)
        {
            await _users.RevokeSessionAsync(id, sessionId);
            return NoContent();
        }
    }
}
