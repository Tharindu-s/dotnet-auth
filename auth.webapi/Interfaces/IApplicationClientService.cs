using auth.webapi.DTO.ApplicationClient;
using auth.webapi.Models;

namespace auth.webapi.Interfaces
{
    // Creates and manages application clients, and verifies the credentials they send
    public interface IApplicationClientService
    {
        Task<ResponseCreateApplicationClientDto> RegisterApplication(CreateApplicationClientDto createDto);

        // Management, for the admin console. None of these see the system (Argus) app.
        Task<List<ApplicationClientDto>> ListAsync();
        Task<ApplicationClientDto> GetAsync(Guid id);
        Task<ApplicationClientDto> UpdateAsync(Guid id, UpdateApplicationClientDto updateDto);
        Task<string> RotateApiKeyAsync(Guid id);
        Task DeleteAsync(Guid id);

        // Returns the app if the ID exists and either the key matches (confidential client) or the
        // origin is allowed (public client). Otherwise throws ApplicationClientAuthenticationException.
        Task<ApplicationClient> AuthenticateAsync(Guid appId, string? apiKey, string? origin);

        string HashApiKey(string rawKey);
        bool VerifyApiKey(string hashedKey, string providedKey);
    }
}
