using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace auth.webapi.Helpers
{
    public static class ExceptionStatusCodeMapper
    {
        public static readonly Dictionary<Type, HttpStatusCode> Map = new()
        {
            { typeof(UserNotFoundException), HttpStatusCode.NotFound },
            { typeof(InvalidCredentialsException), HttpStatusCode.Unauthorized },
            { typeof(AppUnauthorizedException), HttpStatusCode.Unauthorized },
            { typeof(EmailAlreadyExistsException), HttpStatusCode.Conflict },
            { typeof(RefreshTokenExpiredException), HttpStatusCode.Unauthorized },
            // Add more here as needed
        };
    }
}