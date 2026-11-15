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
    // Every management method skips the system (Argus) app: it isn't listed, and it can't be edited or deleted here
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
            var allowedOrigins = NormalizeOrigins(createDto.AllowedOrigins);
            if (createDto.IsPublic && allowedOrigins.Count == 0)
                throw new AppException("A public client needs at least one allowed origin, such as http://localhost:5173.");

            // Public clients run in a browser and can't keep a secret, so they get no key.
            // Otherwise rawkey is given to the caller once and only its hash is stored.
            var rawkey = createDto.IsPublic ? null : GenerateApiKey();

            var applicationClient = new ApplicationClient
            {
                Name = createDto.Name.Trim(),
                ApiKeyHash = rawkey == null ? null : HashApiKey(rawkey),
                IsPublic = createDto.IsPublic,
                AllowedOrigins = allowedOrigins,
                RegistrationEnabled = createDto.RegistrationEnabled
            };
            applicationClient.Audience = AudienceOrDefault(createDto.Audience, applicationClient.Id);

            _context.ApplicationClient.Add(applicationClient);
            await _context.SaveChangesAsync();

            return new ResponseCreateApplicationClientDto
            {
                Id = applicationClient.Id,
                Name = applicationClient.Name,
                Audience = applicationClient.Audience,
                IsPublic = applicationClient.IsPublic,
                RegistrationEnabled = applicationClient.RegistrationEnabled,
                AllowedOrigins = applicationClient.AllowedOrigins,
                CreatedAt = applicationClient.CreatedAt,
                RawApiKey = rawkey
            };
        }

        public async Task<List<ApplicationClientDto>> ListAsync()
        {
            return await ProjectToDto(_context.ApplicationClient.Where(a => !a.IsSystem))
                .OrderBy(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<ApplicationClientDto> GetAsync(Guid id)
        {
            return await ProjectToDto(_context.ApplicationClient.Where(a => a.Id == id && !a.IsSystem))
                .FirstOrDefaultAsync()
                ?? throw new ApplicationClientNotFoundException();
        }

        public async Task<ApplicationClientDto> UpdateAsync(Guid id, UpdateApplicationClientDto updateDto)
        {
            var app = await FindManageableAsync(id);

            var allowedOrigins = NormalizeOrigins(updateDto.AllowedOrigins);
            if (app.IsPublic && allowedOrigins.Count == 0)
                throw new AppException("A public client needs at least one allowed origin, such as http://localhost:5173.");

            app.Name = updateDto.Name.Trim();
            app.Audience = AudienceOrDefault(updateDto.Audience, app.Id);
            app.AllowedOrigins = allowedOrigins;
            app.RegistrationEnabled = updateDto.RegistrationEnabled;
            await _context.SaveChangesAsync();

            return await GetAsync(id);
        }

        public async Task<string> RotateApiKeyAsync(Guid id)
        {
            var app = await FindManageableAsync(id);
            if (app.IsPublic)
                throw new AppException("Public clients don't have an API key.");

            var rawKey = GenerateApiKey();
            app.ApiKeyHash = HashApiKey(rawKey);
            await _context.SaveChangesAsync();
            return rawKey;
        }

        public async Task DeleteAsync(Guid id)
        {
            // The database cascades the delete to the app's users, their roles and refresh tokens
            var deleted = await _context.ApplicationClient
                .Where(a => a.Id == id && !a.IsSystem)
                .ExecuteDeleteAsync();

            if (deleted == 0)
                throw new ApplicationClientNotFoundException();
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

        public bool VerifyApiKey(string hashedKey, string providedKey)
        {
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(HashApiKey(providedKey)),
                Encoding.UTF8.GetBytes(hashedKey));
        }

        // Normalizes, drops blanks and duplicates. Throws AppException for anything that isn't scheme://host[:port].
        public static List<string> NormalizeOrigins(IEnumerable<string>? origins)
        {
            return (origins ?? Enumerable.Empty<string>())
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .Select(NormalizeOrigin)
                .Distinct()
                .ToList();
        }

        private async Task<ApplicationClient> FindManageableAsync(Guid id)
        {
            return await _context.ApplicationClient.FirstOrDefaultAsync(a => a.Id == id && !a.IsSystem)
                ?? throw new ApplicationClientNotFoundException();
        }

        private static IQueryable<ApplicationClientDto> ProjectToDto(IQueryable<ApplicationClient> apps)
        {
            var now = DateTime.UtcNow;
            return apps.Select(a => new ApplicationClientDto
            {
                Id = a.Id,
                Name = a.Name,
                Audience = a.Audience,
                IsPublic = a.IsPublic,
                RegistrationEnabled = a.RegistrationEnabled,
                AllowedOrigins = a.AllowedOrigins,
                CreatedAt = a.CreatedAt,
                UserCount = a.Users.Count,
                ActiveSessionCount = a.RefreshTokens.Count(t => !t.IsRevoked && t.Expires > now)
            });
        }

        // Without an explicit audience, the app's tokens are issued for its own ID
        private static string AudienceOrDefault(string? audience, Guid appId) =>
            string.IsNullOrWhiteSpace(audience) ? appId.ToString() : audience.Trim();

        private static string GenerateApiKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

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
    }
}
