using System.Security.Claims;
using auth.webapi.Helpers;
using auth.webapi.Models;
using auth.webapi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace auth.webapi.Filters
{
    // Admin console endpoints. Use together with [Authorize].
    //
    // The caller must be a user of the Argus app with the Admin role, checked against the database
    // rather than the token's claims. A role claim alone isn't enough: every app's tokens are accepted
    // by this service, so an "Admin" from one of your own apps would otherwise pass.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireArgusAdminAttribute : TypeFilterAttribute
    {
        public RequireArgusAdminAttribute() : base(typeof(ArgusAdminFilter)) { }
    }

    public class ArgusAdminFilter : IAsyncAuthorizationFilter
    {
        private readonly UserManager<AppUser> _userManager;

        public ArgusAdminFilter(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new AppUnauthorizedException();

            var user = await _userManager.Users
                .Include(u => u.App)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || !user.App.IsSystem || !await _userManager.IsInRoleAsync(user, ArgusOptions.AdminRole))
                throw new AppForbiddenException("Only Argus admins can do this.");

            if (await _userManager.IsLockedOutAsync(user))
                throw new AccountLockedException();

            if (user.MustChangePassword)
                throw new PasswordChangeRequiredException();
        }
    }
}
