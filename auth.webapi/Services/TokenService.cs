using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using auth.webapi.Interfaces;
using auth.webapi.Models;
using Microsoft.IdentityModel.Tokens;

namespace auth.webapi.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfigurationHelperService _config;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;

        // using asymmetric encryption and setting up jwks
        private readonly string _privateKeyPath = Path.Combine(AppContext.BaseDirectory, "private_key.pem");
        private readonly RSA _rsa;
        private readonly RsaSecurityKey _rsaKey;

        public TokenService(IConfigurationHelperService config)
        {
            _config = config;
            _jwtIssuer = _config.GetRequiredConfig("JWT:Issuer");
            _jwtAudience = _config.GetRequiredConfig("JWT:Audience");
            var keyId = _config.GetRequiredConfig("JWT:KeyId");

            // loading or generating RSA key
            _rsa = LoadOrCreateRsaKey(_privateKeyPath);
            _rsaKey = new RsaSecurityKey(_rsa)
            {
                KeyId = keyId
            };
        }

        private RSA LoadOrCreateRsaKey(string path)
        {
            if (!File.Exists(path))
            {
                using var rsaGen = RSA.Create(2048);
                File.WriteAllBytes(path, rsaGen.ExportRSAPrivateKey());
            }

            var privateKeyBytes = File.ReadAllBytes(path);
            var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
            return rsa;
        }

        public string CreateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        public string CreateToken(AppUser user)
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
        new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString(), ClaimValueTypes.DateTime)
    };

            var creds = new SigningCredentials(_rsaKey, SecurityAlgorithms.RsaSha256);

            // Token descriptor with expiration, claims, signing credentials, and issuer/audience
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(15), // Set token expiration (15 min)
                SigningCredentials = creds,
                Issuer = _jwtIssuer,
                Audience = _jwtAudience
            };

            Console.WriteLine($"Creating token with expiration: {tokenDescriptor.Expires}, current time: {DateTime.UtcNow}");

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            // Serialize the token to a string for sending to the client
            return tokenHandler.WriteToken(token);
        }

        public object GetJwks()
        {
            var parameters = _rsa.ExportParameters(false); // public key only
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

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _rsaKey,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false,
                ClockSkew = TimeSpan.Zero,
                ValidIssuer = _jwtIssuer,
                ValidAudience = _jwtAudience,
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

                if (securityToken is not JwtSecurityToken jwtToken ||
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.RsaSha256, StringComparison.InvariantCultureIgnoreCase)
)
                {
                    throw new SecurityTokenException("Invalid token");
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }
    }
}