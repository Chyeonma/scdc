using Microsoft.AspNetCore.Mvc;
using Npgsql;
using SCDC.Api.Errors;
using SCDC.BuildingBlocks.Application;

namespace SCDC.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public sealed class HealthController(
    IEnumerable<IModuleDescriptor> modules,
    TimeProvider timeProvider,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(configuration.GetConnectionString("Database"));
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);
            return Ok(new { status = "ready" });
        }
        catch (Exception exception) when (exception is NpgsqlException or TimeoutException or InvalidOperationException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "unready" });
        }
    }

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
