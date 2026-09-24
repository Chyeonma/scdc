using SCDC.Contracts.Identity;

namespace SCDC.Modules.Messaging.Application;

public sealed record CreateDirectConversationCommand(Guid ActorUserId, Guid RecipientUserId);

public sealed record CreateDirectConversationResult(SpaceSummaryDto Space, bool Created);

public sealed record ListSpacesQuery(
    Guid ActorUserId,
    int Limit,
    string? Cursor,
    bool IncludeHidden);

public sealed record SpacePageDto(
    IReadOnlyList<SpaceSummaryDto> Items,
    string? NextCursor,
    bool HasMore);

public sealed record SpaceSummaryDto(
    Guid Id,
    short SpaceType,
    short Status,
    int Version,
    string Name,
    UserSummary? Peer,
    Guid? ServerId,
    MessagePreviewDto? LastMessage,
    string? LastMessageSequence,
    DateTimeOffset? LastActivityAt,
    string? LastReadSequence,
    int UnreadCount,
    SpacePreferencesDto Preferences,
    SpaceCapabilitiesDto Capabilities,
    int NotificationCount = 0);

public sealed record MessagePreviewDto(
    Guid Id,
    string SequenceNo,
    short MessageType,
    UserSummary? Author,
    string? Content,
    DateTimeOffset? DeletedAt);

public sealed record SpacePreferencesDto(
    short NotificationLevel,
    DateTimeOffset? MutedUntil,
    bool IsHidden,
    bool IsPinned);

public sealed record SpaceCapabilitiesDto(
    bool CanRead,
    bool CanSend,
    bool CanEditOwn,
    bool CanDeleteOwn,
    bool CanDeleteOthers,
    bool CanPin,
    bool CanReact,
    bool CanAttach);
