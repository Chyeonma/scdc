using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SCDC.BuildingBlocks.Infrastructure;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;
using SCDC.Contracts.Community;
using SCDC.Contracts.Messaging;

namespace SCDC.Api.Errors;

internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    private static bool IsMessageRoute(string? path)
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
        if (exception is ChannelOutcomeUnknownException unknown)
        {
            var path = $"/api/v1/servers/{unknown.ServerId:D}/channel-operations/{unknown.ClientOperationId:D}";
            var error = Error.ServiceUnavailable("OPERATION_OUTCOME_UNKNOWN", "Channel creation could not be confirmed. Check the original operation.");
            httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            httpContext.Response.Headers.Location = path;
            var problem = ApiProblemDetailsFactory.Create(httpContext, ApiErrorDefaults.FromError(error), error.Description);
            problem.Extensions["statusUrl"] = path;
            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext { HttpContext = httpContext, ProblemDetails = problem });
        }
        if(exception is CommunityChangePendingException pending)
        {
            var path=$"/api/v1/servers/{pending.ServerId:D}/access-operations/{pending.OperationId:D}";
            httpContext.Response.StatusCode=StatusCodes.Status202Accepted;
            httpContext.Response.Headers.Location=path;
            await httpContext.Response.WriteAsJsonAsync(new {operationId=pending.OperationId,phase="fencing",statusUrl=path},cancellationToken);
            return true;
        }
        if (exception is AccessCheckException access)
        {
            if (access.Code == "ACCESS_PENDING" && access.OperationId is Guid operation)
            {
                httpContext.Response.StatusCode = StatusCodes.Status202Accepted;
                var path = $"/api/v1/auth/revocations/{operation:D}";
                httpContext.Response.Headers.Location = path;
                await httpContext.Response.WriteAsJsonAsync(new { operationId = operation, phase = "fencing", version = "1", statusPath = path }, cancellationToken);
                return true;
            }
            var error = access.Code switch
            {
                "SESSION_INVALID" => Error.Unauthorized(access.Code,"The session is no longer valid."),
                "ACCOUNT_ACCESS_DENIED" => Error.Forbidden(access.Code,"An active account with verified primary email is required."),
                "MEMBERSHIP_REQUIRED" => Error.Validation(access.Code,"X-SCDC-Membership-Id must identify your current membership."),
                "Common.Unauthorized" => Error.Unauthorized(access.Code, "The current session is unavailable."),
                "RESOURCE_NOT_FOUND" => Error.NotFound(access.Code, "The requested resource is unavailable."),
                "ACCESS_CHANGED" => Error.Conflict(access.Code, "Access changed. Reload before trying again."),
                _ => Error.ServiceUnavailable(access.Code, "Access cannot be confirmed. Please try again later.")
            };
            var failure = ApiErrorDefaults.FromError(error);
            httpContext.Response.StatusCode = failure.Status;
            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            { HttpContext = httpContext, ProblemDetails = ApiProblemDetailsFactory.Create(httpContext, failure, error.Description) });
        }
        logger.LogError(
            exception,
            "Unhandled exception while processing {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        // Authentication also consults Identity before the DM controller runs.
        var authorityUnavailable = (HttpMethods.IsPost(httpContext.Request.Method) || HttpMethods.IsGet(httpContext.Request.Method))
            && (string.Equals(httpContext.Request.Path.Value?.TrimEnd('/'), "/api/v1/direct-conversations", StringComparison.OrdinalIgnoreCase)
                || IsMessageRoute(httpContext.Request.Path.Value))
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
