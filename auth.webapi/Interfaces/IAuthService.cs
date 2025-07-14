using auth.webapi.DTO.Auth.Register;
using Microsoft.AspNetCore.Identity;

namespace auth.webapi.Interfaces
{
    public interface IAuthService
    {
        Task<(bool IsSuccess, ResponseUserDto? UserDto, IEnumerable<IdentityError>? Errors, string? ExceptionMessage)> RegisterUserAsync(CreateUserDto createUserDto);
    }
}