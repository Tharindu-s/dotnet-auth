using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace auth.webapi.DTO.Auth
{
    public class UserDto
    {
        [Required]
        public required string FullName { get; set; }
        [Required]
        public required string City { get; set; }
    }
}