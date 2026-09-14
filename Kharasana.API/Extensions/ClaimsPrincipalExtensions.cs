using System.Security.Claims;
using Kharasana.Domain.Enums;
using Kharasana.Infrastructure.Authentication;

namespace Kharasana.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetRole(this ClaimsPrincipal user, out UserRole role)
    {
        var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
        return Enum.TryParse(roleClaim, out role);
    }

    public static int? GetFactoryId(this ClaimsPrincipal user)
    {
        var factoryIdClaim = user.FindFirst(CustomClaimTypes.FactoryId)?.Value;
        return int.TryParse(factoryIdClaim, out var factoryId) ? factoryId : null;
    }

    public static int? GetUserId(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}