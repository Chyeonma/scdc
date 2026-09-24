using SCDC.Contracts.Identity;
using SCDC.Contracts.Messaging;

namespace SCDC.Modules.Messaging.Application;

public sealed record CreateGroupConversationCommand(
    Guid ActorUserId,
    string? Name,
    IReadOnlyCollection<Guid>? MemberUserIds,
    int? MaxMembers,
    string? AvatarObjectKey);

public sealed record UpdateGroupConversationCommand(
    Guid ActorUserId,
    Guid SpaceId,
    string? Name,
    string? AvatarObjectKey,
    int? MaxMembers);

public sealed record ChangeGroupOwnerCommand(Guid ActorUserId, Guid SpaceId, Guid NewOwnerUserId);

public sealed record ChangeGroupMemberCommand(Guid ActorUserId, Guid SpaceId, Guid MemberUserId);

public sealed record ChangeGroupMemberRoleCommand(Guid ActorUserId, Guid SpaceId, Guid MemberUserId, short Role);

public sealed record GroupConversationDto(
    Guid SpaceId,
    string Name,
    string? AvatarObjectKey,
    Guid OwnerUserId,
    int? MaxMembers,
    int MemberCount,
    short Status,
    int Version,
    DateTimeOffset? LastActivityAt,
    int UnreadCount = 0,
    string? LastReadSequence = null,
    int NotificationCount = 0,
    UserSpacePreferencesDto? Preferences = null);

public sealed record GroupMemberDto(
    UserSummary User,
    short Role,
    DateTimeOffset JoinedAt);
