using System.Security.Claims;

namespace SCDC.Api.Controllers.Identity;

internal static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        GetRequiredGuidClaim(principal, "sub");

    public static Guid GetSessionId(this ClaimsPrincipal principal) =>
        GetRequiredGuidClaim(principal, "sid");

    public static DateTimeOffset GetAccessExpiresAt(this ClaimsPrincipal principal) =>
        DateTimeOffset.FromUnixTimeSeconds(long.Parse(principal.FindFirst("exp")!.Value, System.Globalization.CultureInfo.InvariantCulture));

    private static Guid GetRequiredGuidClaim(ClaimsPrincipal principal, string claimType)
    {
        var value = principal.FindFirst(claimType)?.Value;
        return Guid.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException($"Required claim '{claimType}' is missing.");
    }
}
