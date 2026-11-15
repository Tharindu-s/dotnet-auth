using System.Security.Cryptography;
using System.Text;
using auth.webapi.Data;
using auth.webapi.DTO.ApplicationClient;
using auth.webapi.Helpers;
using auth.webapi.Interfaces;
using auth.webapi.Models;
using Microsoft.EntityFrameworkCore;

namespace auth.webapi.Services
{
    public class ApplicationClientService : IApplicationClientService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _apiKeySalt;

        public ApplicationClientService(ApplicationDbContext context, IConfigurationHelperService config)
        {
            _context = context;
            _apiKeySalt = config.GetRequiredConfig("Security:ApiKeySalt");
        }

        public string HashApiKey(string rawKey)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiKeySalt));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawKey));
            return Convert.ToBase64String(hash);
        }

        public async Task<ResponseCreateApplicationClientDto> RegisterApplication(CreateApplicationClientDto createDto)
        {
            var allowedOrigins = (createDto.AllowedOrigins ?? new List<string>())
                .Select(NormalizeOrigin)
                .Distinct()
                .ToList();

            if (createDto.IsPublic && allowedOrigins.Count == 0)
                throw new AppException("A public client needs at least one allowed origin, such as http://localhost:5173.");

            // Public clients run in a browser and can't keep a secret, so they get no key.
            // Otherwise rawkey is given to the caller once and only its hash is stored.
            var rawkey = createDto.IsPublic ? null : Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            var applicationClient = new ApplicationClient
            {
                Name = createDto.Name,
                ApiKeyHash = rawkey == null ? null : HashApiKey(rawkey),
                IsPublic = createDto.IsPublic,
                AllowedOrigins = allowedOrigins
            };
            // Without an explicit audience, the app's tokens are issued for its own ID
            applicationClient.Audience = string.IsNullOrWhiteSpace(createDto.Audience)
                ? applicationClient.Id.ToString()
                : createDto.Audience.Trim();

            _context.ApplicationClient.Add(applicationClient);
            await _context.SaveChangesAsync();

            return new ResponseCreateApplicationClientDto
            {
                Id = applicationClient.Id,
                Audience = applicationClient.Audience,
                IsPublic = applicationClient.IsPublic,
                AllowedOrigins = applicationClient.AllowedOrigins,
                RawApiKey = rawkey
            };
        }

        public async Task<ApplicationClient> AuthenticateAsync(Guid appId, string? apiKey, string? origin)
        {
            var app = await _context.ApplicationClient.FirstOrDefaultAsync(a => a.Id == appId);

            if (app == null)
                throw new ApplicationClientAuthenticationException();

            if (app.IsPublic)
            {
                // Browsers always send Origin on these POSTs and scripts can't change it, so this stops
                // other websites from using the App ID. Non-browser clients can fake it: a public client
                // is identified, not authenticated.
                if (!TryNormalizeOrigin(origin, out var normalized) || !app.AllowedOrigins.Contains(normalized))
                    throw new ApplicationClientAuthenticationException("This application only accepts requests from its allowed origins.");
            }
            else if (string.IsNullOrEmpty(apiKey) || app.ApiKeyHash == null || !VerifyApiKey(app.ApiKeyHash, apiKey))
            {
                throw new ApplicationClientAuthenticationException();
            }

            return app;
        }

        // "HTTP://LocalHost:5173/" -> "http://localhost:5173", the form browsers send in the Origin header
        private static string NormalizeOrigin(string origin)
        {
            if (!TryNormalizeOrigin(origin, out var normalized))
                throw new AppException($"'{origin}' is not a valid origin. Use scheme://host[:port], such as https://app.example.com.");
            return normalized;
        }

        private static bool TryNormalizeOrigin(string? origin, out string normalized)
        {
            normalized = "";
            if (!Uri.TryCreate(origin?.Trim(), UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                || uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query))
                return false;

            normalized = uri.GetLeftPart(UriPartial.Authority).ToLowerInvariant();
            return true;
        }

        public bool VerifyApiKey(string hashedKey, string providedKey)
        {
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(HashApiKey(providedKey)),
                Encoding.UTF8.GetBytes(hashedKey));
        }
    }
}
