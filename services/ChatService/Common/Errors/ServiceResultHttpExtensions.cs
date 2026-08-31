using ChatService.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Common.Errors;

internal static class ServiceResultHttpExtensions
{
    public static IActionResult ToActionResult<T>(this ControllerBase controller, ServiceResult<T> result)
    {
        return result.Status switch
        {
            ServiceStatus.Ok => controller.Ok(result.Value),
            ServiceStatus.Created => controller.StatusCode(StatusCodes.Status201Created, result.Value),
            ServiceStatus.NoContent => controller.NoContent(),
            ServiceStatus.ValidationError => ValidationProblem(controller, result),
            ServiceStatus.AuthenticationFailed => controller.Problem(
                type: ProblemTypes.AuthenticationFailed,
                title: "Authentication failed.",
                detail: result.Detail,
                statusCode: StatusCodes.Status401Unauthorized),
            ServiceStatus.Forbidden => controller.Problem(
                type: ProblemTypes.Forbidden,
                title: "The operation is forbidden.",
                detail: result.Detail,
                statusCode: StatusCodes.Status403Forbidden),
            ServiceStatus.NotFound => controller.Problem(
                type: ProblemTypes.NotFound,
                title: "Resource not found.",
                detail: result.Detail,
                statusCode: StatusCodes.Status404NotFound),
            ServiceStatus.Conflict => controller.Problem(
                type: ProblemTypes.Conflict,
                title: "The resource conflicts with existing data.",
                detail: result.Detail,
                statusCode: StatusCodes.Status409Conflict),
            ServiceStatus.TooManyRequests => controller.Problem(
                type: ProblemTypes.RateLimitExceeded,
                title: "Too many requests.",
                detail: result.Detail,
                statusCode: StatusCodes.Status429TooManyRequests),
            _ => throw new InvalidOperationException($"Unsupported service status: {result.Status}.")
        };
    }

    private static IActionResult ValidationProblem<T>(ControllerBase controller, ServiceResult<T> result)
    {
        var errors = result.Errors?.ToDictionary(pair => pair.Key, pair => pair.Value)
            ?? new Dictionary<string, string[]>();
        var problemDetails = new ValidationProblemDetails(errors)
        {
            Type = ProblemTypes.ValidationError,
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Detail = "The request contains invalid fields."
        };
        ApiProblemDetails.ApplyDefaults(problemDetails, controller.HttpContext);
        var response = new BadRequestObjectResult(problemDetails);
        response.ContentTypes.Add("application/problem+json");
        return response;
    }
}
