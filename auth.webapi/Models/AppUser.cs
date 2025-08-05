using Microsoft.AspNetCore.Identity;

namespace auth.webapi.Models
{
    public class AppUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? City { get; set; }

        public Guid ApplicationClientId { get; set; }
        public ApplicationClient App { get; set; } = null!;

        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}