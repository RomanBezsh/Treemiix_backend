using System.Security.Claims;

namespace CloneAmazonBack.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        return Guid.Parse(userId);
    }
}
