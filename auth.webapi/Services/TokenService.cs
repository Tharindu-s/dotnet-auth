using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using auth.webapi.Interfaces;
using auth.webapi.Models;
using Microsoft.IdentityModel.Tokens;

namespace auth.webapi.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfigurationHelperService _config;
        // SymmetricSecurityKey is used for symmetric encryption, which means the same key is used for both encryption and decryption.
        private readonly SymmetricSecurityKey _key;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;

        public TokenService(IConfigurationHelperService config)
        {
            _config = config;
            var signInKey = _config.GetRequiredConfig("JWT:SigningKey");
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signInKey));
            _jwtIssuer = _config.GetRequiredConfig("JWT:Issuer");
            _jwtAudience = _config.GetRequiredConfig("JWT:Audience");
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

            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);

            // Token descriptor with expiration, claims, signing credentials, and issuer/audience
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(2), // Set token expiration (2 min)
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

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _key,
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
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha512, StringComparison.InvariantCultureIgnoreCase)
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