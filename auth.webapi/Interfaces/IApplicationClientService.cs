using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using auth.webapi.DTO.ApplicationClient;

// this is the interface responsible for creating application IDs

namespace auth.webapi.Interfaces
{
    public interface IApplicationClientService
    {
        Task<ResponseCreateApplicationClientDto> RegisterApplication(CreateApplicationClientDto createDto);

        string HashApiKey(string rawKey);
        bool VerifyApiKey(string hashedKey, string providedKey);
    }
}