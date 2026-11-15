namespace auth.webapi.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }

        // SHA-256 of the raw token. The raw token only ever exists in the client's cookie.
        public string TokenHash { get; set; } = null!;

        // Every token created by rotating the same login shares a family.
        // Presenting an already-rotated token revokes the whole family.
        public Guid FamilyId { get; set; }

        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime Expires { get; set; }
        public bool IsRevoked { get; set; } = false;
        public DateTime? RevokedAt { get; set; }

        // Set only when the token was revoked by a refresh. Null for logout and family revocation.
        public int? ReplacedByTokenId { get; set; }

        public string Device { get; set; } = null!;
        public string IPAddress { get; set; } = null!;

        public Guid ApplicationClientId { get; set; }
        public ApplicationClient App { get; set; } = null!;

        // Foreign key to the user
        public string UserId { get; set; } = null!;
        public AppUser User { get; set; } = null!;
    }
}
