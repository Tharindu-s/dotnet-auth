using System.ComponentModel.DataAnnotations;

namespace auth.webapi.DTO.Auth
{
    public class ChangePasswordDto
    {
        [Required]
        public required string CurrentPassword { get; set; }

        [Required]
        public required string NewPassword { get; set; }
    }
}
