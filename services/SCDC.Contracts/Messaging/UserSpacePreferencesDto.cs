using System.Text.Json.Serialization;

namespace SCDC.Contracts.Messaging;

public sealed record UserSpacePreferencesDto(
    [property: JsonRequired] short NotificationLevel,
    [property: JsonRequired] DateTimeOffset? MutedUntil,
    [property: JsonRequired] bool IsHidden,
    [property: JsonRequired] bool IsPinned);
