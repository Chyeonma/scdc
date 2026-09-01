using Microsoft.AspNetCore.Mvc;
using SCDC.Api.Errors;
using SCDC.BuildingBlocks.Application.Results;

namespace SCDC.Api.Controllers;

[ApiController]
[ProducesResponseType(typeof(ApiValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
[ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
[ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
[ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status429TooManyRequests, "application/problem+json")]
[ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
[ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult<TValue> FromResult<TValue>(Result<TValue> result) =>
        result.IsSuccess
            ? Ok(result.Value)
            : ApiErrorMapper.ToObjectResult(result.Error, HttpContext);

    protected ActionResult<TValue> FromCreatedResult<TValue>(
        Result<TValue> result,
        string actionName,
        object? routeValues = null) =>
        result.IsSuccess
            ? CreatedAtAction(actionName, routeValues, result.Value)
            : ApiErrorMapper.ToObjectResult(result.Error, HttpContext);

    protected IActionResult FromNoContentResult(Result result) =>
        result.IsSuccess
            ? NoContent()
            : ApiErrorMapper.ToObjectResult(result.Error, HttpContext);
}
