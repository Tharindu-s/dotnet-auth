using System.Collections.Immutable;
using System.Net;

namespace auth.webapi.Helpers
{
    public static class ExceptionStatusCodeMapper
    {
        public static readonly ImmutableDictionary<Type, HttpStatusCode> Map =
            new Dictionary<Type, HttpStatusCode>
            {
                { typeof(UserNotFoundException), HttpStatusCode.NotFound },
                { typeof(InvalidCredentialsException), HttpStatusCode.Unauthorized },
                { typeof(AppUnauthorizedException), HttpStatusCode.Unauthorized },
                { typeof(EmailAlreadyExistsException), HttpStatusCode.Conflict },
                { typeof(RefreshTokenExpiredException), HttpStatusCode.Unauthorized },
                { typeof(UserCreationFailedException), HttpStatusCode.BadRequest},
                { typeof(InvalidTokenException), HttpStatusCode.Unauthorized }
            }.ToImmutableDictionary();
    }
}