using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Auth;

namespace Kharasana.Application.Interfaces.Services;

public interface IAuthService
{
    Task<ApiResponse<object>> RegisterAsync(RegisterUserDto request);

    Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request);

    Task<ApiResponse<LoginResponseDto>> RefreshTokenAsync(string refreshToken);

    Task<ApiResponse<object>> RevokeTokenAsync(string refreshToken);

    Task<ApiResponse<object>> ActivateAccountAsync(string tokenHash, string newPassword);
}