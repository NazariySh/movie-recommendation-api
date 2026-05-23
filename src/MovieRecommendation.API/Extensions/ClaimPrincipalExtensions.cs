using System.Security.Claims;

namespace MovieRecommendation.API.Extensions;

public static class ClaimPrincipalExtensions
{
    public static Guid GetId(this ClaimsPrincipal claimsPrincipal)
    {
        var id = GetIdOrDefault(claimsPrincipal);

        if (id is null)
        {
            throw new UnauthorizedAccessException("User id not found");
        }

        return id.Value;
    }

    public static Guid? GetIdOrDefault(this ClaimsPrincipal claimsPrincipal)
    {
        var id = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(id, out var result) ? result : null;
    }

    public static bool IsAuthenticated(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal.Identity?.IsAuthenticated ?? false;
    }
}
