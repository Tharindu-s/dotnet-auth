namespace auth.webapi.DTO.Admin
{
    public class AdminSessionDto
    {
        public int TokenId { get; set; }
        public required string UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? Device { get; set; }
        public string? IPAddress { get; set; }
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
