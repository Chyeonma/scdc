using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;

namespace SCDC.Api.Errors;

internal sealed class ApiAuthorizationResultHandler(
    IProblemDetailsService problemDetailsService) : IAuthorizationMiddlewareResultHandler
{
    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Succeeded)
        {
            await next(context);
            return;
        }

        var status = authorizeResult.Forbidden
            ? StatusCodes.Status403Forbidden
            : StatusCodes.Status401Unauthorized;
        var descriptor = ApiErrorDefaults.FromStatusCode(status);
        context.Response.StatusCode = status;

        if (status == StatusCodes.Status401Unauthorized)
        {
            context.Response.Headers.WWWAuthenticate = "Bearer";
        }

        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = ApiProblemDetailsFactory.Create(
                context,
                descriptor,
                status == StatusCodes.Status401Unauthorized
                    ? "A valid access token is required."
                    : "The authenticated user does not have permission to perform this action.")
        });
    }
}
