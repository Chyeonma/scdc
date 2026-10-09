using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SCDC.BuildingBlocks.Infrastructure;
using SCDC.BuildingBlocks.Application.Results;

namespace SCDC.Api.Errors;

internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    private static bool IsMessageSend(string? path)
    {
        var parts = path?.Trim('/').Split('/');
        return parts is { Length: 5 } && parts[0].Equals("api", StringComparison.OrdinalIgnoreCase)
            && parts[1].Equals("v1", StringComparison.OrdinalIgnoreCase)
            && parts[2].Equals("direct-conversations", StringComparison.OrdinalIgnoreCase)
            && Guid.TryParse(parts[3], out _) && parts[4].Equals("messages", StringComparison.OrdinalIgnoreCase);
    }
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "Unhandled exception while processing {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        // Authentication also consults Identity before the DM controller runs.
        var authorityUnavailable = (HttpMethods.IsPost(httpContext.Request.Method) || HttpMethods.IsGet(httpContext.Request.Method))
            && (string.Equals(httpContext.Request.Path.Value?.TrimEnd('/'), "/api/v1/direct-conversations", StringComparison.OrdinalIgnoreCase)
                || (HttpMethods.IsPost(httpContext.Request.Method) && IsMessageSend(httpContext.Request.Path.Value)))
            && DatabaseAvailability.IsUnavailable(exception);
        var detail = authorityUnavailable
            ? "The service cannot confirm this request. Please try again."
            : "An unexpected error occurred while processing the request.";
        var descriptor = authorityUnavailable
            ? ApiErrorDefaults.FromError(Error.ServiceUnavailable("AUTHORITY_UNAVAILABLE", detail))
            : ApiErrorDefaults.Unexpected;
        httpContext.Response.StatusCode = descriptor.Status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = ApiProblemDetailsFactory.Create(
                httpContext,
                descriptor,
                detail)
        });
    }
}
