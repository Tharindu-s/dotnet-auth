namespace auth.webapi.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = null!;
        public DateTime Expires { get; set; }
        public bool IsRevoked { get; set; } = false;

        public string Device { get; set; } = null!;
        public string IPAddress { get; set; } = null!;

        public Guid ApplicationClientId { get; set; }
        public ApplicationClient App { get; set; } = null!;

        // Foreign key to the user
        public string UserId { get; set; } = null!;
        public AppUser User { get; set; } = null!;
    }
}