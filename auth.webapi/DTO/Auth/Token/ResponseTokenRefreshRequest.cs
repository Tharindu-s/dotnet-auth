using System.ComponentModel.DataAnnotations;

namespace auth.webapi.DTO.Auth.Token
{
    public class ResponseTokenRefreshRequest
    {
        [Required]
        public required string AccessToken { get; set; }
        [Required]
        public required string RefreshToken { get; set; }
        public DateTime RefreshTokenExpiry { get; set; }
    }
}
