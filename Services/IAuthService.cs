using TaskManagement.API.DTOs;

namespace TaskManagement.API.Services;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);
}
