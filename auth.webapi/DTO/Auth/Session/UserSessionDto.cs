namespace auth.webapi.DTO.Auth.Session
{
    public class UserSessionDto
    {
        public int TokenId { get; set; }
        public string? Device { get; set; }
        public string? IPAddress { get; set; }
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }

        // True for the session whose refresh cookie came with this request
        public bool IsCurrent { get; set; }
    }
}
