namespace auth.webapi.DTO.ApplicationClient
{
    // An app as the admin console sees it
    public class ApplicationClientDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public bool IsPublic { get; set; }
        public bool RegistrationEnabled { get; set; }
        public List<string> AllowedOrigins { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public int UserCount { get; set; }
        public int ActiveSessionCount { get; set; }
    }
}
