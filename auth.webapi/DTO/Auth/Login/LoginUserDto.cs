using System.ComponentModel.DataAnnotations;

namespace auth.webapi.DTO.Auth.Login
{
    public class LoginUserDto
    {
        [Required]
        public required string Email { get; set; }

        [Required]
        public required string Password { get; set; }
    }
}