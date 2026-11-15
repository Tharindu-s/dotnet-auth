namespace auth.webapi.DTO.Admin
{
    // A user of one of the apps, as the admin console sees them
    public class AdminUserDto
    {
        public required string Id { get; set; }
        public required string Email { get; set; }
        public string? FullName { get; set; }
        public string? City { get; set; }
        public bool IsLockedOut { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public int ActiveSessionCount { get; set; }
        public DateTime? LastSignInAt { get; set; }
    }
}
