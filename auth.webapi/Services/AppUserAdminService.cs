using auth.webapi.Data;
using auth.webapi.DTO.Admin;
using auth.webapi.Helpers;
using auth.webapi.Interfaces;
using auth.webapi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace auth.webapi.Services
{
    public class AppUserAdminService : IAppUserAdminService
    {
        public const int MaxPageSize = 100;
        private const int MaxSessions = 200;

        // An admin lock lasts until an admin unlocks the user
        private static readonly TimeSpan AdminLockDuration = TimeSpan.FromDays(365 * 100);

        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public AppUserAdminService(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<PagedResult<AdminUserDto>> ListUsersAsync(Guid appId, string? search, int page, int pageSize)
        {
            await EnsureManageableAppAsync(appId);
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var users = _context.Users.Where(u => u.ApplicationClientId == appId);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToUpperInvariant();
                users = users.Where(u => u.NormalizedEmail!.Contains(term) || (u.FullName != null && u.FullName.ToUpper().Contains(term)));
            }

            var total = await users.CountAsync();
            var now = DateTime.UtcNow;
            var items = await users
                .OrderBy(u => u.NormalizedEmail)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new AdminUserDto
                {
                    Id = u.Id,
                    Email = u.Email!,
                    FullName = u.FullName,
                    City = u.City,
                    LockoutEnd = u.LockoutEnd,
                    ActiveSessionCount = u.RefreshTokens.Count(t => !t.IsRevoked && t.Expires > now),
                    LastSignInAt = u.RefreshTokens.Max(t => (DateTime?)t.Created)
                })
                .ToListAsync();

            // Compared here rather than in SQL, because SQLite can't compare DateTimeOffset values
            var nowOffset = DateTimeOffset.UtcNow;
            foreach (var item in items)
                item.IsLockedOut = item.LockoutEnd > nowOffset;

            return new PagedResult<AdminUserDto> { Items = items, Total = total, Page = page, PageSize = pageSize };
        }

        public async Task LockUserAsync(Guid appId, string userId)
        {
            var user = await FindUserAsync(appId, userId);

            await _userManager.SetLockoutEnabledAsync(user, true);
            var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.Add(AdminLockDuration));
            ThrowIfFailed(result);

            await RevokeTokensAsync(_context.RefreshTokens.Where(rt => rt.UserId == user.Id));
        }

        public async Task UnlockUserAsync(Guid appId, string userId)
        {
            var user = await FindUserAsync(appId, userId);

            ThrowIfFailed(await _userManager.SetLockoutEndDateAsync(user, null));
            ThrowIfFailed(await _userManager.ResetAccessFailedCountAsync(user));
        }

        public async Task DeleteUserAsync(Guid appId, string userId)
        {
            // The database cascades the delete to the user's roles and refresh tokens
            var user = await FindUserAsync(appId, userId);
            ThrowIfFailed(await _userManager.DeleteAsync(user));
        }

        public async Task<List<AdminSessionDto>> ListSessionsAsync(Guid appId, string? userId)
        {
            await EnsureManageableAppAsync(appId);
            var now = DateTime.UtcNow;

            var sessions = _context.RefreshTokens
                .Where(rt => rt.ApplicationClientId == appId && !rt.IsRevoked && rt.Expires > now);
            if (!string.IsNullOrEmpty(userId))
                sessions = sessions.Where(rt => rt.UserId == userId);

            return await sessions
                .OrderByDescending(rt => rt.Created)
                .Take(MaxSessions)
                .Select(rt => new AdminSessionDto
                {
                    TokenId = rt.Id,
                    UserId = rt.UserId,
                    UserEmail = rt.User.Email,
                    Device = rt.Device,
                    IPAddress = rt.IPAddress,
                    IssuedAt = rt.Created,
                    ExpiresAt = rt.Expires
                })
                .ToListAsync();
        }

        public async Task RevokeSessionAsync(Guid appId, int sessionId)
        {
            await EnsureManageableAppAsync(appId);

            var revoked = await RevokeTokensAsync(_context.RefreshTokens
                .Where(rt => rt.Id == sessionId && rt.ApplicationClientId == appId));

            if (revoked == 0)
                throw new SessionNotFoundException();
        }

        private async Task EnsureManageableAppAsync(Guid appId)
        {
            if (!await _context.ApplicationClient.AnyAsync(a => a.Id == appId && !a.IsSystem))
                throw new ApplicationClientNotFoundException();
        }

        private async Task<AppUser> FindUserAsync(Guid appId, string userId)
        {
            await EnsureManageableAppAsync(appId);
            return await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId && u.ApplicationClientId == appId)
                ?? throw new UserNotFoundException();
        }

        private static Task<int> RevokeTokensAsync(IQueryable<RefreshToken> tokens)
        {
            return tokens
                .Where(rt => !rt.IsRevoked)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(rt => rt.IsRevoked, true)
                    .SetProperty(rt => rt.RevokedAt, DateTime.UtcNow));
        }

        private static void ThrowIfFailed(IdentityResult result)
        {
            if (!result.Succeeded)
                throw new AppException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }
    }
}
