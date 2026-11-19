using System.Security.Claims;
using Kharasana.Application.Common;
using Kharasana.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;

namespace Kharasana.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    public string? UserName => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);
    public string? Role => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);
    public int? FactoryId
    {
        get
        {
            var factoryIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue(CustomClaimTypes.FactoryId);
            return int.TryParse(factoryIdClaim, out var factoryId) ? factoryId : null;
        }
    }

    public bool IsInRole(string roleName)
    {
        return _httpContextAccessor.HttpContext?.User?.IsInRole(roleName) ?? false;
    }
}
