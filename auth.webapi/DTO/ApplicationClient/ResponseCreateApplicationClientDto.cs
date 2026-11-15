
namespace auth.webapi.DTO.ApplicationClient
{
    public class ResponseCreateApplicationClientDto
    {
        public Guid Id { get; set; }
        public string Audience { get; set; } = null!;
        public bool IsPublic { get; set; }
        public List<string> AllowedOrigins { get; set; } = new();
        public string? RawApiKey { get; set; } // wont be stored anywhere and will be shown once. Null for public clients.
    }
}
