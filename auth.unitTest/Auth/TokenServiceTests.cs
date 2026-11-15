using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using auth.webapi.Models;
using auth.webapi.Services;

namespace auth.unitTest.Auth
{
    public class TokenServiceTests : IAsyncLifetime
    {
        private TestServices _t = null!;

        public async Task InitializeAsync() => _t = await TestServices.CreateAsync();
        public async Task DisposeAsync() => await _t.DisposeAsync();

        private static readonly AppUser User = new()
        {
            Id = "user-1",
            Email = "jane@example.com",
            UserName = "jane@example.com"
        };

        private static readonly ApplicationClient App = new()
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Notes",
            Audience = "notes-api"
        };

        private JwtSecurityToken Create(params string[] roles) =>
            new JwtSecurityTokenHandler().ReadJwtToken(_t.Tokens.CreateToken(User, App, roles));

        [Fact]
        public void Token_HasAppAudienceAndAuthServiceAudience()
        {
            var token = Create();

            Assert.Equal(new[] { "notes-api", TestServices.AuthAudience }, token.Audiences.OrderBy(a => a == TestServices.AuthAudience));
        }

        [Fact]
        public void Token_HasAppIdAndRoleClaims()
        {
            var token = Create("User", "Admin");

            Assert.Equal(App.Id.ToString(), token.Claims.Single(c => c.Type == TokenService.AppIdClaimType).Value);
            Assert.Equal(new[] { "Admin", "User" }, token.Claims.Where(c => c.Type == "role").Select(c => c.Value).Order());
        }

        [Fact]
        public void Token_IatIsUnixTimestamp()
        {
            var token = Create();

            var iat = Assert.Single(token.Claims, c => c.Type == JwtRegisteredClaimNames.Iat);
            Assert.True(long.TryParse(iat.Value, out _));
        }

        [Fact]
        public void Token_ExpiresInFifteenMinutes()
        {
            var token = Create();

            Assert.InRange(token.ValidTo - DateTime.UtcNow, TimeSpan.FromMinutes(14), TimeSpan.FromMinutes(15));
        }

        [Fact]
        public void Token_KidMatchesJwks()
        {
            var token = Create();
            var jwks = JsonSerializer.SerializeToElement(_t.Tokens.GetJwks());

            var key = jwks.GetProperty("keys")[0];
            Assert.Equal(token.Header.Kid, key.GetProperty("kid").GetString());
            Assert.Equal(_t.SigningKey.KeyId, token.Header.Kid);
            Assert.False(key.TryGetProperty("d", out _)); // no private key material
        }

        [Fact]
        public void RefreshTokens_AreRandomAndHashDeterministically()
        {
            var a = _t.Tokens.CreateRefreshToken();
            var b = _t.Tokens.CreateRefreshToken();

            Assert.NotEqual(a, b);
            Assert.Equal(_t.Tokens.HashRefreshToken(a), _t.Tokens.HashRefreshToken(a));
            Assert.NotEqual(_t.Tokens.HashRefreshToken(a), _t.Tokens.HashRefreshToken(b));
        }
    }
}
