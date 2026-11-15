using Microsoft.IdentityModel.Tokens;

namespace auth.webapi.Interfaces
{
    // Holds the single RSA key used to sign and validate access tokens
    public interface ISigningKeyService
    {
        RsaSecurityKey Key { get; }
    }
}
