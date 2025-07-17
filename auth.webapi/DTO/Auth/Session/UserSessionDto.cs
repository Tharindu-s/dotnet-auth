using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace auth.webapi.DTO.Auth.Session
{
    public class UserSessionDto
    {
        public int TokenId { get; set; }  // or GUID
        public string? Device { get; set; }
        public string? IPAddress { get; set; }
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}