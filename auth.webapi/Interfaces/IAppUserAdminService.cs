using auth.webapi.DTO.Admin;

namespace auth.webapi.Interfaces
{
    // The admin console's view of one app's users and sessions. Throws ApplicationClientNotFoundException
    // for unknown apps and for the system (Argus) app.
    public interface IAppUserAdminService
    {
        Task<PagedResult<AdminUserDto>> ListUsersAsync(Guid appId, string? search, int page, int pageSize);

        // Locking also signs the user out everywhere, since refresh doesn't check the lockout
        Task LockUserAsync(Guid appId, string userId);
        Task UnlockUserAsync(Guid appId, string userId);
        Task DeleteUserAsync(Guid appId, string userId);

        Task<List<AdminSessionDto>> ListSessionsAsync(Guid appId, string? userId);
        Task RevokeSessionAsync(Guid appId, int sessionId);
    }
}
