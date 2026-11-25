using Microsoft.AspNetCore.Mvc;
using SCDC.Identity.Api.Errors;
using SCDC.BuildingBlocks.Application;
using SCDC.Modules.Identity.Application;

namespace SCDC.Identity.Api.Controllers;

[ApiController]
[Route("api/v1/health/identity")]
public sealed class HealthController(
    IEnumerable<IModuleDescriptor> modules,
    TimeProvider timeProvider) : ControllerBase
{
    [HttpGet("access")]
    public async Task<IActionResult> Access([FromServices] IIdentityReadiness readiness, CancellationToken cancellationToken) =>
        await readiness.IsReadyAsync(cancellationToken) ? Ok(new { status = "ready" }) : StatusCode(503, new { status = "unavailable" });

    [HttpGet]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public ActionResult<HealthResponse> Get()
    {
        var moduleStates = modules
            .OrderBy(module => module.Name)
            .Select(module => new ModuleHealth(
                module.Name,
                module.DatabaseSchema,
                module.Stage.ToString()))
            .ToArray();

        return Ok(new HealthResponse(
            "healthy",
            timeProvider.GetUtcNow(),
            moduleStates));
    }
}

public sealed record HealthResponse(
    string Status,
    DateTimeOffset Timestamp,
    IReadOnlyList<ModuleHealth> Modules);

public sealed record ModuleHealth(string Name, string DatabaseSchema, string Stage);
