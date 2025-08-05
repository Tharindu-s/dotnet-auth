using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using auth.webapi.Data;
using auth.webapi.DTO.ApplicationClient;
using auth.webapi.Interfaces;
using auth.webapi.Models;

namespace auth.webapi.Services
{
    public class ApplicationClientService : IApplicationClientService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _apiKeySalt;
        private readonly IConfigurationHelperService _config;

        public ApplicationClientService(ApplicationDbContext context, IConfigurationHelperService config)
        {
            _context = context;
            _config = config;
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
            var rawkey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var appId = Guid.NewGuid();
            var hashedKey = HashApiKey(rawkey);

            var applicationClient = new ApplicationClient
            {
                AppId = appId,
                Name = createDto.Name,
                ApiKeyHash = hashedKey
            };

            _context.ApplicationClient.Add(applicationClient);
            await _context.SaveChangesAsync();

            return new ResponseCreateApplicationClientDto
            {
                AppId = appId,
                RawApiKey = rawkey
            };
        }

        public bool VerifyApiKey(string hashedKey, string providedKey)
        {
            return HashApiKey(providedKey) == hashedKey;
        }
    }
}