using System.ComponentModel.DataAnnotations;
using auth.webapi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace auth.webapi.Services
{
    // Replaces Identity's UserValidator, which checks username uniqueness across every app.
    // Same checks, but uniqueness is per ApplicationClientId.
    public class AppScopedUserValidator : IUserValidator<AppUser>
    {
        public async Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user)
        {
            var errors = new List<IdentityError>();
            var describer = manager.ErrorDescriber;

            var userName = await manager.GetUserNameAsync(user);
            var allowedCharacters = manager.Options.User.AllowedUserNameCharacters;
            if (string.IsNullOrWhiteSpace(userName))
            {
                errors.Add(describer.InvalidUserName(userName));
            }
            else if (!string.IsNullOrEmpty(allowedCharacters) && userName.Any(c => !allowedCharacters.Contains(c)))
            {
                errors.Add(describer.InvalidUserName(userName));
            }
            else
            {
                // NormalizedUserName isn't set yet during CreateAsync, so normalize here
                var normalizedUserName = manager.NormalizeName(userName);
                var taken = await manager.Users.AnyAsync(u =>
                    u.ApplicationClientId == user.ApplicationClientId &&
                    u.NormalizedUserName == normalizedUserName &&
                    u.Id != user.Id);
                if (taken)
                    errors.Add(describer.DuplicateUserName(userName));
            }

            var email = await manager.GetEmailAsync(user);
            if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
            {
                errors.Add(describer.InvalidEmail(email));
            }
            else
            {
                var normalizedEmail = manager.NormalizeEmail(email);
                var taken = await manager.Users.AnyAsync(u =>
                    u.ApplicationClientId == user.ApplicationClientId &&
                    u.NormalizedEmail == normalizedEmail &&
                    u.Id != user.Id);
                if (taken)
                    errors.Add(describer.DuplicateEmail(email));
            }

            return errors.Count > 0 ? IdentityResult.Failed(errors.ToArray()) : IdentityResult.Success;
        }
    }
}
