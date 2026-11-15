using System.ComponentModel.DataAnnotations;

namespace auth.webapi.DTO.Auth.Register
{
    public class CreateUserDto
    {
        [Required]
        [MaxLength(100)]
        public required string FullName { get; set; }
        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public required string Email { get; set; }
        [Required]
        [MaxLength(100)]
        public required string City { get; set; }
        [Required]
        public required string Password { get; set; }

    }
}
