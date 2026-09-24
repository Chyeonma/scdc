using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCDC.Api.Controllers.Identity;
using SCDC.Modules.Community.Application;

namespace SCDC.Api.Controllers.Community;

[Authorize]
[Route("api/v1/servers")]
public sealed class ServersController(ICommunityService community) : ApiControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<ServerDto>>> List(CancellationToken ct) => FromResult(await community.ListServersAsync(User.GetUserId(), ct));
    [HttpPost] public async Task<ActionResult<ServerDto>> Create(CreateServerRequest request, CancellationToken ct) => FromCreatedResult(await community.CreateServerAsync(new(User.GetUserId(), request.Name, request.Description), ct), "/api/v1/servers");
    [HttpGet("{serverId:guid}/channels")] public async Task<ActionResult<IReadOnlyList<ChannelDto>>> Channels(Guid serverId, [FromQuery] bool includeHidden, CancellationToken ct) => FromResult(await community.ListChannelsAsync(User.GetUserId(), serverId, includeHidden, ct));
    [HttpPost("{serverId:guid}/channels")] public async Task<ActionResult<ChannelDto>> CreateChannel(Guid serverId, CreateChannelRequest request, CancellationToken ct) => FromCreatedResult(await community.CreateChannelAsync(new(User.GetUserId(), serverId, request.Name, request.Topic, request.Visibility), ct), $"/api/v1/servers/{serverId}/channels");
    [HttpGet("{serverId:guid}/members")] public async Task<ActionResult<IReadOnlyList<CommunityMemberDto>>> Members(Guid serverId, CancellationToken ct) => FromResult(await community.ListMembersAsync(User.GetUserId(), serverId, ct));
    [HttpPost("{serverId:guid}/invites")] public async Task<ActionResult<InviteDto>> Invite(Guid serverId, CreateInviteRequest request, CancellationToken ct) => FromCreatedResult(await community.CreateInviteAsync(new(User.GetUserId(), serverId, request.MaxUses, request.ExpiresAt), ct), $"/api/v1/servers/{serverId}/invites");
    [HttpPost("{serverId:guid}/leave")] public async Task<IActionResult> Leave(Guid serverId, CancellationToken ct) => FromNoContentResult(await community.LeaveAsync(User.GetUserId(), serverId, ct));
    [HttpPost("{serverId:guid}/members/{userId:guid}/kick")] public async Task<IActionResult> Kick(Guid serverId, Guid userId, CancellationToken ct) => FromNoContentResult(await community.KickAsync(User.GetUserId(), serverId, userId, ct));
    [HttpPost("{serverId:guid}/members/{userId:guid}/ban")] public async Task<IActionResult> Ban(Guid serverId, Guid userId, BanRequest request, CancellationToken ct) => FromNoContentResult(await community.BanAsync(User.GetUserId(), serverId, userId, request.Reason, ct));
    [HttpPost("{serverId:guid}/roles")] public async Task<ActionResult<RoleDto>> CreateRole(Guid serverId, CreateRoleRequest request, CancellationToken ct) => FromCreatedResult(await community.CreateRoleAsync(new(User.GetUserId(), serverId, request.Name, request.PermissionCodes), ct), $"/api/v1/servers/{serverId}/roles");
    [HttpPut("{serverId:guid}/members/{userId:guid}/roles/{roleId:guid}")] public async Task<IActionResult> Role(Guid serverId, Guid userId, Guid roleId, [FromQuery] bool assigned, CancellationToken ct) => FromNoContentResult(await community.SetRoleAsync(User.GetUserId(), serverId, userId, roleId, assigned, ct));
    [HttpPut("{serverId:guid}/channels/{spaceId:guid}/overrides")] public async Task<IActionResult> Override(Guid serverId, Guid spaceId, ChannelOverrideRequest request, CancellationToken ct) => FromNoContentResult(await community.SetChannelOverrideAsync(new(User.GetUserId(), serverId, spaceId, request.RoleId, request.UserId, request.PermissionCode, request.Effect), ct));
    [HttpPost("{serverId:guid}/channels/{spaceId:guid}/archive")] public async Task<IActionResult> Archive(Guid serverId, Guid spaceId, CancellationToken ct) => FromNoContentResult(await community.ArchiveChannelAsync(User.GetUserId(), serverId, spaceId, false, ct));
    [HttpDelete("{serverId:guid}/channels/{spaceId:guid}")] public async Task<IActionResult> DeleteChannel(Guid serverId, Guid spaceId, CancellationToken ct) => FromNoContentResult(await community.ArchiveChannelAsync(User.GetUserId(), serverId, spaceId, true, ct));
}

[Authorize]
[Route("api/v1/invites")]
public sealed class InvitesController(ICommunityService community) : ApiControllerBase
{
    [HttpPost("{code}/join")] public async Task<ActionResult<ServerDto>> Join(string code, CancellationToken ct) => FromResult(await community.JoinInviteAsync(User.GetUserId(), code, ct));
}

public sealed record CreateServerRequest([param: Required, StringLength(100, MinimumLength = 2)] string Name, [param: StringLength(500)] string? Description);
public sealed record CreateChannelRequest([param: Required, StringLength(100, MinimumLength = 1)] string Name, [param: StringLength(500)] string? Topic, [param: Range(1, 3)] short Visibility);
public sealed record CreateInviteRequest([param: Range(1, int.MaxValue)] int? MaxUses, DateTimeOffset? ExpiresAt);
public sealed record BanRequest([param: StringLength(500)] string? Reason);
public sealed record CreateRoleRequest([param: Required, StringLength(50, MinimumLength = 1)] string Name, IReadOnlyCollection<string> PermissionCodes);
public sealed record ChannelOverrideRequest(Guid? RoleId, Guid? UserId, [param: Required, StringLength(50)] string PermissionCode, [param: Range(1, 2)] short Effect);
