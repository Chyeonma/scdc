using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SCDC.Api.Controllers;
using SCDC.BuildingBlocks.Application.Results;

namespace SCDC.Api.Tests.Infrastructure;

[Route("_tests/responses")]
public sealed class TestResponseController : ApiControllerBase
{
    [HttpGet("not-found")]
    public ActionResult<string> GetNotFound() => FromResult(
        Result.Failure<string>(
            Error.NotFound("Identity.UserNotFound", "User was not found.")));

    [HttpGet("validation")]
    public IActionResult GetValidation([FromQuery, Required] string value) => Ok(value);

    [HttpGet("exception")]
    public IActionResult GetException() =>
        throw new InvalidOperationException("Sensitive exception detail.");
}
