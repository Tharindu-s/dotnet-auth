namespace auth.webapi.Models
{
    public class ApplicationClient
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = null!;
        public Guid AppId { get; set; } = Guid.NewGuid(); // Public-facing App ID
        public string ApiKeyHash { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}