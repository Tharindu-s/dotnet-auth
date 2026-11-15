namespace auth.webapi.Models
{
    public class ApplicationClient
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = null!;

        // Null for public clients, which have no API key
        public string? ApiKeyHash { get; set; }

        // A public client is a browser app (SPA) that can't keep a secret. It authenticates with
        // X-App-Id only, and requests must come from one of AllowedOrigins.
        public bool IsPublic { get; set; }
        public List<string> AllowedOrigins { get; set; } = new();

        // The aud claim for this app's access tokens, such as "notes-api".
        // The app's own API validates it so it only accepts tokens issued for it.
        public string Audience { get; set; } = null!;

        // When false, POST /api/auth/register is rejected and only existing users can sign in
        public bool RegistrationEnabled { get; set; } = true;

        // True only for the Argus app, which the service creates for its own admin console on first start.
        // It's hidden from the apps API and can't be edited or deleted there.
        public bool IsSystem { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
