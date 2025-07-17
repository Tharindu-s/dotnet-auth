using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace auth.webapi.DTO.Auth
{
    public class AuthenticatedUser
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
    }
}