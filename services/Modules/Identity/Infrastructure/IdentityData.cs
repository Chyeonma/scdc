using System.Net;
using System.Text.Json;
using SCDC.Modules.Identity.Application;
using SCDC.Modules.Identity.Domain;

namespace SCDC.Modules.Identity.Infrastructure;

internal static class IdentityData
{
    public static UserAccountResponse ToResponse(User user)
    {
        var email = user.Emails.Single(item => item.IsPrimary);
        var profile = user.Profile
            ?? throw new InvalidOperationException("User profile is missing.");

        return new UserAccountResponse(
            user.Id,
            user.Username,
            profile.DisplayName,
            email.Email,
            email.VerifiedAt is not null,
            ToStatusName(user.Status),
            profile.Bio,
            profile.AvatarObjectKey,
            profile.Locale,
            profile.Timezone,
            user.CreatedAt,
            user.UpdatedAt,
            user.Version);
    }

    public static SecurityEvent SecurityEvent(
        Guid? userId,
        string eventType,
        RequestContext context,
        DateTimeOffset occurredAt,
        object? metadata = null) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            EventType = eventType,
            IpAddress = ParseIp(context.IpAddress),
            UserAgent = Truncate(context.UserAgent, 500),
            Metadata = JsonSerializer.Serialize(metadata ?? new { }),
            OccurredAt = occurredAt
        };

    public static OutboxEvent Outbox(
        string eventType,
        Guid aggregateId,
        int? aggregateVersion,
        DateTimeOffset occurredAt,
        object payload) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            EventType = eventType,
            AggregateType = "User",
            AggregateId = aggregateId,
            AggregateVersion = aggregateVersion,
            Payload = JsonSerializer.Serialize(payload),
            OccurredAt = occurredAt,
            AvailableAt = occurredAt,
            AttemptCount = 0
        };

    public static IPAddress? ParseIp(string? value) =>
        IPAddress.TryParse(value, out var address) ? address : null;

    public static string? Truncate(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Length <= maxLength ? value : value[..maxLength];

    public static string ToStatusName(UserStatus status) => status switch
    {
        UserStatus.PendingVerification => "pending_verification",
        UserStatus.Active => "active",
        UserStatus.Suspended => "suspended",
        UserStatus.Disabled => "disabled",
        UserStatus.Deleted => "deleted",
        _ => "unknown"
    };

    public static void RevokeSession(
        AuthSession session,
        DateTimeOffset now,
        string reason)
    {
        session.RevokedAt ??= now;
        session.RevokeReason ??= reason;

        foreach (var token in session.RefreshTokens.Where(token => token.RevokedAt is null))
        {
            token.RevokedAt = now;
            token.RevokeReason = reason;
        }
    }
}
