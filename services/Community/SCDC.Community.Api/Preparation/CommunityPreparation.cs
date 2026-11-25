using Microsoft.AspNetCore.Mvc;

namespace SCDC.Community.Api.Preparation;

internal static class CommunityPreparation
{
    public const string ErrorCode = "COMMUNITY_INTEGRATIONS_NOT_CONFIGURED";

    public static Task WriteUnavailableAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers.CacheControl = "no-store";
        return context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = 503,
            Title = "Community is not ready for business traffic.",
            Detail = "The preparation host is waiting for verified service integrations.",
            Extensions = { ["code"] = ErrorCode }
        }, options: null, contentType: "application/problem+json", cancellationToken: context.RequestAborted);
    }
}
