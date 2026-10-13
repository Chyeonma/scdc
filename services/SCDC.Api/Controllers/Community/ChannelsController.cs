using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCDC.Api.Errors;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;
using SCDC.Modules.Community.Features.Channels.Application;

namespace SCDC.Api.Controllers.Community;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateChannelRequest(Guid ClientOperationId, string? Name, string? Topic, string? Kind);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateChannelRequest(string? ExpectedVersion, JsonElement Name, JsonElement Topic);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ReplaceChannelAccessRequest(string? ExpectedAccessVersion, string? DefaultView,
    RoleOverride[]? RoleOverrides, MemberOverride[]? MemberOverrides);

[Authorize]
[Route("api/v1/servers/{serverId:guid}/channels")]
public sealed class ChannelsController(IChannelService channels) : ApiControllerBase
{
    private AccountActor Actor => new(Claim("sub"), Claim("sid"), Claim("sst"));
    private Guid Claim(string name) => Guid.TryParse(User.FindFirst(name)?.Value, out var id) ? id : throw new InvalidOperationException($"Required claim '{name}' is missing.");
    [HttpGet]
    public async Task<ActionResult<ChannelPage>> List(Guid serverId, [FromQuery] int limit = 20, [FromQuery] string? cursor = null, CancellationToken ct = default)
        => FromResult(await channels.ListAsync(Actor, serverId, limit, cursor, ct));
    [HttpGet("{channelId:guid}")]
    public async Task<ActionResult<ChannelView>> Get(Guid serverId, Guid channelId, CancellationToken ct)
        => FromResult(await channels.GetAsync(Actor, serverId, channelId, ct));
    [HttpPost]
    [RequestSizeLimit(16_384)]
    public async Task<ActionResult<ChannelView>> Create(Guid serverId, CreateChannelRequest request, CancellationToken ct)
    {
        var result = await channels.CreateAsync(Actor, serverId, new(request.ClientOperationId, request.Name, request.Topic, request.Kind), ct);
        if (result.IsFailure) return ApiErrorMapper.ToObjectResult(result.Error, HttpContext);
        Response.Headers.Location = $"/api/v1/servers/{serverId}/channels/{result.Value.Channel.Id}";
        return result.Value.Created ? StatusCode(201, result.Value.Channel) : Ok(result.Value.Channel);
    }
    [HttpPatch("{channelId:guid}")]
    [RequestSizeLimit(16_384)]
    public async Task<ActionResult<ChannelView>> Update(Guid serverId, Guid channelId, UpdateChannelRequest request, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        var hasName = request.Name.ValueKind != JsonValueKind.Undefined; var hasTopic = request.Topic.ValueKind != JsonValueKind.Undefined;
        if (hasName && request.Name.ValueKind != JsonValueKind.String) errors["name"] = ["Name must be a string."];
        if (hasTopic && request.Topic.ValueKind is not (JsonValueKind.String or JsonValueKind.Null)) errors["topic"] = ["Topic must be a string or null."];
        if (errors.Count > 0) return ApiErrorMapper.ToObjectResult(new ValidationError("VALIDATION_FAILED", "Invalid channel data.", errors), HttpContext);
        return FromResult(await channels.UpdateAsync(Actor, serverId, channelId,
            new(request.ExpectedVersion, hasName, hasName ? request.Name.GetString() : null, hasTopic, hasTopic ? request.Topic.GetString() : null), ct));
    }
    [HttpGet("{channelId:guid}/access")]
    public async Task<ActionResult<ChannelAccess>> Access(Guid serverId, Guid channelId, CancellationToken ct)
        => FromResult(await channels.AccessAsync(Actor, serverId, channelId, ct));
    [HttpPut("{channelId:guid}/access")]
    [RequestSizeLimit(65_536)]
    public async Task<ActionResult<ChannelAccess>> Replace(Guid serverId, Guid channelId, ReplaceChannelAccessRequest request, CancellationToken ct)
        => FromResult(await channels.ReplaceAccessAsync(Actor, serverId, channelId,
            new(request.ExpectedAccessVersion, request.DefaultView, request.RoleOverrides, request.MemberOverrides), ct));
}
