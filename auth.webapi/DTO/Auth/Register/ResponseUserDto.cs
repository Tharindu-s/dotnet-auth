using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace auth.webapi.DTO.Auth.Register
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
        public DateTime RefreshTokenExpiryTime { get; set; }
    }
}