using System.ComponentModel.DataAnnotations;

namespace auth.webapi.DTO.Auth
{
    public class ResponseUserDto
    {
        [Required]
        public required string Id { get; set; }
        [Required]
        public required string FullName { get; set; }
        [Required]
        public required string UserName { get; set; }
        [Required]
        public required string Email { get; set; }
        [Required]
        public required string City { get; set; }
        [Required]
        public required string AccessToken { get; set; }
        [Required]
        public required string RefreshToken { get; set; }
        public DateTime RefreshTokenExpiry { get; set; }
    }
}