using Microsoft.AspNetCore.Mvc;

namespace SCDC.Community.Api.Preparation;

internal static class CommunityPreparation
{
    public const string ErrorCode = "COMMUNITY_INTEGRATIONS_NOT_CONFIGURED";

    public static Task WriteUnavailableAsync(HttpContext context) => WriteAsync(context, ErrorCode,
        "Community is not ready for business traffic.", "The preparation host is waiting for verified service integrations.");

    public static Task WriteDependencyUnavailableAsync(HttpContext context) => WriteAsync(context, "ACCESS_CHECK_UNAVAILABLE",
        "Community dependencies are unavailable.", "A required database or service is not ready.");

    private static Task WriteAsync(HttpContext context, string code, string title, string detail)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers.CacheControl = "no-store";
        return context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = 503,
            Title = title,
            Detail = detail,
            Extensions = { ["code"] = code }
        }, options: null, contentType: "application/problem+json", cancellationToken: context.RequestAborted);
    }
}
