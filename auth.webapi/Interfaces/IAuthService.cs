using auth.webapi.DTO.Auth.Login;
using auth.webapi.DTO.Auth.Register;
using Microsoft.AspNetCore.Identity;

namespace auth.webapi.Interfaces
{
    public interface IAuthService
    {
        Task<ResponseUserDto> RegisterUserAsync(CreateUserDto createUserDto);

        Task<ResponseUserDto> LoginUserAsync(LoginUserDto loginDto);

    }
}