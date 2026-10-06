using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCDC.Api.Errors;
using SCDC.Contracts.Identity;
using SCDC.Modules.Community.Features.Servers.Application;

namespace SCDC.Api.Controllers.Community;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateServerRequest(Guid ClientOperationId, string? Name, string? Description, string? Visibility = "public");

[Authorize]
[Route("api/v1/servers")]
public sealed class ServersController(IServerService servers) : ApiControllerBase
{
    private AccountActor Actor => new(Claim("sub"), Claim("sid"), Claim("sst"));
    private Guid Claim(string name) => Guid.TryParse(User.FindFirst(name)?.Value, out var id) ? id
        : throw new InvalidOperationException($"Required claim '{name}' is missing.");

    [HttpPost]
    [RequestSizeLimit(16_384)]
    [ProducesResponseType(typeof(ServerDetail), 201)]
    [ProducesResponseType(typeof(ServerDetail), 200)]
    public async Task<ActionResult<ServerDetail>> Create(CreateServerRequest request, CancellationToken ct)
    {
        var result = await servers.CreateAsync(Actor, new(request.ClientOperationId, request.Name, request.Description, request.Visibility), ct);
        if (result.IsFailure)
            return ApiErrorMapper.ToObjectResult(result.Error, HttpContext);
        var location = $"/api/v1/servers/{result.Value.Server.Id}";
        if (result.Value.Created)
            return Created(location, result.Value.Server);
        Response.Headers.Location = location;
        return Ok(result.Value.Server);
    }
    [HttpGet]
    public async Task<ActionResult<MyServerPage>> List([FromQuery] int limit = 20, [FromQuery] string? cursor = null, CancellationToken ct = default)
        => FromResult(await servers.ListAsync(Actor, limit, cursor, ct));
    [HttpGet("{serverId:guid}")]
    [ProducesResponseType(typeof(ServerDetail), 200)]
    public async Task<ActionResult<ServerSummary>> Get(Guid serverId, CancellationToken ct)
        => FromResult(await servers.GetAsync(Actor, serverId, ct));
    [HttpGet("{serverId:guid}/membership/me")]
    public async Task<ActionResult<MembershipView>> Membership(Guid serverId, CancellationToken ct)
        => FromResult(await servers.GetMembershipAsync(Actor, serverId, ct));
}
