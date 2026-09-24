using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;

namespace SCDC.Modules.Community.Application;

public interface ICommunityService
{
    Task<Result<IReadOnlyList<ServerDto>>> ListServersAsync(Guid actorUserId, CancellationToken cancellationToken);
    Task<Result<ServerDto>> CreateServerAsync(CreateServerCommand command, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<ChannelDto>>> ListChannelsAsync(Guid actorUserId, Guid serverId, CancellationToken cancellationToken);
    Task<Result<ChannelDto>> CreateChannelAsync(CreateChannelCommand command, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<CommunityMemberDto>>> ListMembersAsync(Guid actorUserId, Guid serverId, CancellationToken cancellationToken);
    Task<Result<InviteDto>> CreateInviteAsync(CreateInviteCommand command, CancellationToken cancellationToken);
    Task<Result<ServerDto>> JoinInviteAsync(Guid actorUserId, string code, CancellationToken cancellationToken);
    Task<Result> LeaveAsync(Guid actorUserId, Guid serverId, CancellationToken cancellationToken);
    Task<Result> KickAsync(Guid actorUserId, Guid serverId, Guid targetUserId, CancellationToken cancellationToken);
    Task<Result> BanAsync(Guid actorUserId, Guid serverId, Guid targetUserId, string? reason, CancellationToken cancellationToken);
    Task<Result> SetRoleAsync(Guid actorUserId, Guid serverId, Guid targetUserId, Guid roleId, bool assigned, CancellationToken cancellationToken);
    Task<Result<RoleDto>> CreateRoleAsync(CreateRoleCommand command, CancellationToken cancellationToken);
    Task<Result> SetChannelOverrideAsync(SetChannelOverrideCommand command, CancellationToken cancellationToken);
    Task<Result> ArchiveChannelAsync(Guid actorUserId, Guid serverId, Guid spaceId, bool deleted, CancellationToken cancellationToken);
}

public sealed record CreateServerCommand(Guid ActorUserId, string Name, string? Description);
public sealed record CreateChannelCommand(Guid ActorUserId, Guid ServerId, string Name, string? Topic, short Visibility);
public sealed record CreateInviteCommand(Guid ActorUserId, Guid ServerId, int? MaxUses, DateTimeOffset? ExpiresAt);
public sealed record SetChannelOverrideCommand(Guid ActorUserId, Guid ServerId, Guid SpaceId, Guid? RoleId, Guid? UserId, string PermissionCode, short Effect);
public sealed record CreateRoleCommand(Guid ActorUserId, Guid ServerId, string Name, IReadOnlyCollection<string> PermissionCodes);
public sealed record ServerDto(Guid Id, string Name, string Slug, string? Description, Guid OwnerUserId, short Status);
public sealed record ChannelDto(Guid SpaceId, Guid ServerId, string Name, string? Topic, short Visibility, int Position, short Status, bool CanRead, bool CanSend);
public sealed record CommunityMemberDto(Guid UserId, UserSummary? User, string? Nickname, string RoleName, short Status);
public sealed record InviteDto(string Code, DateTimeOffset? ExpiresAt, int? MaxUses);
public sealed record RoleDto(Guid Id, Guid ServerId, string Name, int Position, IReadOnlyList<string> PermissionCodes);
