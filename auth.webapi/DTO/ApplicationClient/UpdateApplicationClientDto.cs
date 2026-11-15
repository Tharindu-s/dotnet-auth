using System.ComponentModel.DataAnnotations;

namespace auth.webapi.DTO.ApplicationClient
{
    // Whether an app is public or confidential can't change: it decides whether the app has an API key
    public class UpdateApplicationClientDto
    {
        [Required]
        [MaxLength(100)]
        public required string Name { get; set; }

        // Changing it makes the app's API reject tokens issued before the change. Blank means the app ID.
        [MaxLength(200)]
        public string? Audience { get; set; }

        public List<string>? AllowedOrigins { get; set; }

        public bool RegistrationEnabled { get; set; }
    }
}
