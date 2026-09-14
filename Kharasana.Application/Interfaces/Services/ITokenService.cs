using Kharasana.Application.DTOs.Auth;
using Kharasana.Domain.Entities;

namespace Kharasana.Application.Interfaces.Services;

public interface ITokenService
{
    TokenResultDto GenerateToken(User user);
}