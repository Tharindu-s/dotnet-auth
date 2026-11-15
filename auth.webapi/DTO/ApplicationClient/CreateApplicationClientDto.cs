using System.ComponentModel.DataAnnotations;

namespace auth.webapi.DTO.ApplicationClient
{
    public class CreateApplicationClientDto
    {
        [Required]
        [MaxLength(100)]
        public required string Name { get; set; }

        // The aud claim this app's tokens are issued for, such as "notes-api". Defaults to the app ID.
        [MaxLength(200)]
        public string? Audience { get; set; }

        // Public clients (browser apps) get no API key and must list the origins they run on
        public bool IsPublic { get; set; }
        public List<string>? AllowedOrigins { get; set; }
    }
}
