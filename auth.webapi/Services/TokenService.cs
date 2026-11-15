using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using auth.webapi.Interfaces;
using auth.webapi.Models;
using Microsoft.IdentityModel.Tokens;

namespace auth.webapi.Services
{
    public class TokenService : ITokenService
    {
        public const string AppIdClaimType = "app_id";

        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly RsaSecurityKey _rsaKey;

        public TokenService(IConfigurationHelperService config, ISigningKeyService signingKeyService)
        {
            _jwtIssuer = config.GetRequiredConfig("JWT:Issuer");
            _jwtAudience = config.GetRequiredConfig("JWT:Audience");
            _rsaKey = signingKeyService.Key;
        }

        public string CreateRefreshToken()
        {
            return Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        }

        // Refresh tokens are 64 random bytes, so a plain SHA-256 is enough. No salt needed.
        public string HashRefreshToken(string refreshToken)
        {
            return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
        }

        public string CreateToken(AppUser user, ApplicationClient app, IEnumerable<string> roles)
        {
            if (user.Email == null || user.UserName == null)
                throw new InvalidOperationException("User email or username is missing.");

            // Add claims. It's better to use ClaimTypes for standard claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(AppIdClaimType, app.Id.ToString())
            };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            // aud holds the app's own API plus this auth service, which needs to accept
            // the token for its own endpoints (sessions, logout, me)
            foreach (var audience in new[] { app.Audience, _jwtAudience }.Distinct())
                claims.Add(new Claim(JwtRegisteredClaimNames.Aud, audience));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(_rsaKey, SecurityAlgorithms.RsaSha256),
                Issuer = _jwtIssuer
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public object GetJwks()
        {
            var parameters = _rsaKey.Rsa.ExportParameters(false); // public key only
            var jwk = new
            {
                kty = "RSA",
                use = "sig",
                kid = _rsaKey.KeyId,
                alg = "RS256",
                n = Base64UrlEncoder.Encode(parameters.Modulus),
                e = Base64UrlEncoder.Encode(parameters.Exponent)
            };

            return new { keys = new[] { jwk } };
        }
    }
}
