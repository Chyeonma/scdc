using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Common.Errors;

internal static class ApiProblemDetails
{
    public static void ApplyDefaults(
        ProblemDetails problemDetails,
        HttpContext httpContext)
    {
        problemDetails.Instance ??= httpContext.Request.Path.Value;

        if (!problemDetails.Extensions.ContainsKey("traceId"))
        {
            problemDetails.Extensions["traceId"] =
                Activity.Current?.Id ?? httpContext.TraceIdentifier;
        }
    }
}
