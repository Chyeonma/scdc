using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SCDC.Api.Errors;

internal static class ProblemDetailsServiceCollectionExtensions
{
    public static IServiceCollection AddApiProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
                ApiProblemDetailsFactory.Enrich(context.ProblemDetails, context.HttpContext);
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ApiAuthorizationResultHandler>();
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = actionContext =>
            {
                var errors = actionContext.ModelState
                    .Where(entry => entry.Value?.Errors.Count > 0)
                    .ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value!.Errors
                            .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                                ? "The supplied value is invalid."
                                : error.ErrorMessage)
                            .ToArray());

                var problemDetails = ApiProblemDetailsFactory.CreateValidation(
                    actionContext.HttpContext,
                    errors);
                var result = new BadRequestObjectResult(problemDetails);
                result.ContentTypes.Add("application/problem+json");

                return result;
            };
        });

        return services;
    }
}
