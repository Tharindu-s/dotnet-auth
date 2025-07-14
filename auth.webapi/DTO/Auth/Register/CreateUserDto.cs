using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace auth.webapi.DTO.Auth.Register
{
    public class CreateUserDto
    {
        [Required]
        public required string FullName { get; set; }
        [Required]
        public required string Email { get; set; }
        [Required]
        public required string City { get; set; }
        [Required]
        public required string Password { get; set; }

    }
}