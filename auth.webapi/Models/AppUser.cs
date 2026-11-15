using Microsoft.AspNetCore.Identity;

namespace auth.webapi.Models
{
    public class AppUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? City { get; set; }

        // Set on the seeded Argus admin. Admin endpoints are refused until the password is changed.
        public bool MustChangePassword { get; set; }

        public Guid ApplicationClientId { get; set; }
        public ApplicationClient App { get; set; } = null!;

        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}