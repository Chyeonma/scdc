using Microsoft.EntityFrameworkCore;
using SCDC.Modules.Identity.Infrastructure.Persistence;

namespace SCDC.Modules.Identity.Infrastructure.Email;

internal sealed class IdentityMaintenance(IdentityDbContext dbContext, TimeProvider timeProvider)
{
    private static readonly string[] SecurityEventTypes =
    [
        "registration_succeeded", "email_verification_requested", "email_verified", "password_reset_requested",
        "password_reset_succeeded", "password_changed", "login_failed", "login_succeeded",
        "refresh_token_reuse_detected", "refresh_token_rotated", "logout", "logout_all", "session_revoked"
    ];

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var cutoff = timeProvider.GetUtcNow().AddDays(-7);
        var userIds = await dbContext.AuthSessions.AsNoTracking()
            .Where(session => (session.RevokedAt <= cutoff || session.ExpiresAt <= cutoff)
                && (session.DeviceName != null || session.UserAgent != null || session.CreatedByIp != null
                    || session.LastSeenIp != null || session.RefreshTokens.Any()))
            .Select(session => session.UserId)
            .Concat(dbContext.AccountTokens.Where(token => token.ConsumedAt != null
                ? token.ConsumedAt <= cutoff : token.ExpiresAt <= cutoff).Select(token => token.UserId))
            .Distinct().Order().Take(100).ToArrayAsync(cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        foreach (var userId in userIds) await dbContext.LockUserAsync(userId, cancellationToken);
        var terminalTokens = dbContext.RefreshTokens.Where(token => userIds.Contains(token.Session.UserId)
            && (token.Session.RevokedAt <= cutoff || token.Session.ExpiresAt <= cutoff));
        // Break both directions before deleting the terminal family; active families keep
        // their used/replaced tokens so replay detection continues to work.
        await terminalTokens.ExecuteUpdateAsync(setters => setters
            .SetProperty(token => token.ParentTokenId, (Guid?)null)
            .SetProperty(token => token.ReplacedByTokenId, (Guid?)null), cancellationToken);
        await terminalTokens.ExecuteDeleteAsync(cancellationToken);
        await dbContext.AuthSessions.Where(session => userIds.Contains(session.UserId)
                && (session.RevokedAt <= cutoff || session.ExpiresAt <= cutoff))
            .ExecuteUpdateAsync(setters => setters.SetProperty(session => session.DeviceName, (string?)null)
                .SetProperty(session => session.UserAgent, (string?)null)
                .SetProperty(session => session.CreatedByIp, (System.Net.IPAddress?)null)
                .SetProperty(session => session.LastSeenIp, (System.Net.IPAddress?)null), cancellationToken);
        await dbContext.AccountTokens.Where(token => userIds.Contains(token.UserId)
            && (token.ConsumedAt != null ? token.ConsumedAt <= cutoff : token.ExpiresAt <= cutoff))
            .ExecuteDeleteAsync(cancellationToken);
        // Session IDs, expiry/revoke state and user security stamps remain as deny markers.
        await transaction.CommitAsync(cancellationToken);

        await dbContext.EmailDeliveries.Where(delivery => delivery.TerminalAt <= cutoff).ExecuteDeleteAsync(cancellationToken);
        // FK SET NULL removes only the active-token pointer, never LastIssuedAt/cooldown.
        await dbContext.SecurityEvents.Where(item => SecurityEventTypes.Contains(item.EventType) && item.OccurredAt <= cutoff
                && (item.IpAddress != null || item.UserAgent != null))
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IpAddress, (System.Net.IPAddress?)null)
                .SetProperty(item => item.UserAgent, (string?)null), cancellationToken);
        var auditCutoff = timeProvider.GetUtcNow().AddDays(-90);
        await dbContext.SecurityEvents.Where(item => SecurityEventTypes.Contains(item.EventType) && item.OccurredAt <= auditCutoff)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.OutboxEvents.Where(item => item.PublishedAt <= cutoff
                && (item.EventType == "Identity.EmailVerificationRequested" || item.EventType == "Identity.PasswordResetRequested")
                && item.Payload != "{}")
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Payload, "{}"), cancellationToken);
    }
}
