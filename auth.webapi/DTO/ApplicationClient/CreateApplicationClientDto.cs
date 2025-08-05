using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace auth.webapi.DTO.ApplicationClient
{
    public class CreateApplicationClientDto
    {
        public required string Name { get; set; }
    }
}