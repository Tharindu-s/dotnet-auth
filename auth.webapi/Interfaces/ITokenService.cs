using auth.webapi.Models;

namespace auth.webapi.Interfaces
{
    public interface ITokenService
    {
        string CreateToken(AppUser user, ApplicationClient app, IEnumerable<string> roles);
        string CreateRefreshToken();
        string HashRefreshToken(string refreshToken);
        object GetJwks();
    }
}
