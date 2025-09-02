using System.Security.Claims;
using auth.webapi.Models;

namespace auth.webapi.Interfaces
{
    public interface ITokenService
    {
        string CreateToken(AppUser user);
        string CreateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
        object GetJwks();
    }
}