using Microsoft.AspNetCore.Mvc;
using SCDC.Api.Errors;
using SCDC.BuildingBlocks.Application;

namespace SCDC.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public sealed class HealthController(
    IEnumerable<IModuleDescriptor> modules,
    TimeProvider timeProvider) : ControllerBase
{
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
