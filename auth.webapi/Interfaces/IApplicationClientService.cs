using auth.webapi.DTO.ApplicationClient;
using auth.webapi.Models;

// this is the interface responsible for creating application IDs

namespace auth.webapi.Interfaces
{
    public interface IApplicationClientService
    {
        Task<ResponseCreateApplicationClientDto> RegisterApplication(CreateApplicationClientDto createDto);

        // Returns the app if the ID exists and either the key matches (confidential client) or the
        // origin is allowed (public client). Otherwise throws ApplicationClientAuthenticationException.
        Task<ApplicationClient> AuthenticateAsync(Guid appId, string? apiKey, string? origin);

        string HashApiKey(string rawKey);
        bool VerifyApiKey(string hashedKey, string providedKey);

    }
}
