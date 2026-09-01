using Microsoft.AspNetCore.Mvc;
using SCDC.BuildingBlocks.Application.Results;

namespace SCDC.Api.Errors;

internal static class ApiErrorMapper
{
    public static ObjectResult ToObjectResult(Error error, HttpContext httpContext)
    {
        var descriptor = ApiErrorDefaults.FromError(error);

        ProblemDetails problemDetails = error is ValidationError validationError
            ? ApiProblemDetailsFactory.CreateValidation(
                httpContext,
                validationError.Errors.ToDictionary(entry => entry.Key, entry => entry.Value),
                validationError.Code,
                validationError.Description)
            : ApiProblemDetailsFactory.Create(httpContext, descriptor, error.Description);

        var result = new ObjectResult(problemDetails)
        {
            StatusCode = descriptor.Status
        };
        result.ContentTypes.Add("application/problem+json");

        return result;
    }
}
