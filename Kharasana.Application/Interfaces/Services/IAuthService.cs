using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Auth;

namespace Kharasana.Application.Interfaces.Services;

public interface IAuthService
{
    Task<ApiResponse<object>> RegisterAsync(RegisterUserDto request);

    Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request);
}