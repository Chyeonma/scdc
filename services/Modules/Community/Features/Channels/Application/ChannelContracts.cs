using System.Text.Json.Serialization;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;

namespace SCDC.Modules.Community.Features.Channels.Application;

public sealed record ChannelView(Guid Id, Guid ServerId, string Name, string? Topic, string Kind,
    DateTimeOffset CreatedAt, string Version, string AccessVersion);
public sealed record ChannelPage(IReadOnlyList<ChannelView> Items, string? NextCursor);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RoleOverride(Guid RoleId, string? Effect);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record MemberOverride(Guid UserId, Guid MembershipId, string? Effect);
public sealed record ChannelAccess(Guid ServerId, Guid ChannelId, string AccessVersion, string DefaultView,
    IReadOnlyList<RoleOverride> RoleOverrides, IReadOnlyList<MemberOverride> MemberOverrides);
public sealed record CreateChannelCommand(Guid ClientOperationId, string? Name, string? Topic, string? Kind);
public sealed record UpdateChannelCommand(string? ExpectedVersion, bool HasName, string? Name, bool HasTopic, string? Topic);
public sealed record ReplaceChannelAccessCommand(string? ExpectedAccessVersion, string? DefaultView,
    IReadOnlyList<RoleOverride>? RoleOverrides, IReadOnlyList<MemberOverride>? MemberOverrides);
public sealed record CreateChannelResult(ChannelView Channel, bool Created);

public interface IChannelService
{
    Task<Result<ChannelPage>> ListAsync(AccountActor actor, Guid server, int limit, string? cursor, CancellationToken ct);
    Task<Result<ChannelView>> GetAsync(AccountActor actor, Guid server, Guid channel, CancellationToken ct);
    Task<Result<CreateChannelResult>> CreateAsync(AccountActor actor, Guid server, CreateChannelCommand command, CancellationToken ct);
    Task<Result<ChannelView>> UpdateAsync(AccountActor actor, Guid server, Guid channel, UpdateChannelCommand command, CancellationToken ct);
    Task<Result<ChannelAccess>> AccessAsync(AccountActor actor, Guid server, Guid channel, CancellationToken ct);
    Task<Result<ChannelAccess>> ReplaceAccessAsync(AccountActor actor, Guid server, Guid channel, ReplaceChannelAccessCommand command, CancellationToken ct);
}
