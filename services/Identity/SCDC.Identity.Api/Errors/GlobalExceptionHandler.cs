using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SCDC.Modules.Identity.Infrastructure;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;

namespace SCDC.Identity.Api.Errors;

internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
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

        var descriptor = DatabaseAvailability.IsUnavailable(exception)
            ? ApiErrorDefaults.FromError(Error.ServiceUnavailable("AUTHORITY_UNAVAILABLE", "Identity is unavailable. Please try again."))
            : ApiErrorDefaults.Unexpected;
        httpContext.Response.StatusCode = descriptor.Status;
        var detail = descriptor == ApiErrorDefaults.Unexpected
            ? "An unexpected error occurred while processing the request."
            : "Identity is unavailable. Please try again.";

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
