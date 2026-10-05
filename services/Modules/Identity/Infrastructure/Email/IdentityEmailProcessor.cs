using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SCDC.Modules.Identity.Domain;
using SCDC.Modules.Identity.Infrastructure.Persistence;

namespace SCDC.Modules.Identity.Infrastructure.Email;

internal sealed class IdentityEmailProcessor(
    IdentityDbContext dbContext,
    AccountEmailQueue queue,
    IAccountEmailSender sender,
    IOptions<IdentityEmailOptions> options,
    TimeProvider timeProvider)
{
    private readonly IdentityEmailOptions _options = options.Value;

    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await SuppressInvalidAsync(cancellationToken);
        if (!_options.Enabled) return 0;

        var count = 0;
        for (; count < _options.BatchSize; count++)
        {
            var owner = Guid.NewGuid();
            var now = timeProvider.GetUtcNow();
            var leaseUntil = now.AddSeconds(_options.LeaseSeconds);
            // A short atomic claim; no database transaction is held while talking to Gmail.
            var ids = await dbContext.Database.SqlQuery<Guid>($"""
                WITH candidate AS (
                    SELECT id FROM identity.email_deliveries
                    WHERE status IN (0, 1, 2) AND next_attempt_at <= {now}
                        AND envelope_expires_at > {now}
                        AND (status <> 1 OR lease_until <= {now})
                    ORDER BY next_attempt_at, created_at
                    FOR UPDATE SKIP LOCKED LIMIT 1
                )
                UPDATE identity.email_deliveries delivery
                SET status = 1, lease_owner = {owner}, lease_until = {leaseUntil}
                FROM candidate WHERE delivery.id = candidate.id
                RETURNING delivery.id AS "Value"
                """).ToListAsync(cancellationToken);
            if (ids.Count == 0) break;

            var delivery = await dbContext.EmailDeliveries.AsNoTracking()
                .SingleAsync(item => item.Id == ids[0], cancellationToken);
            await ProcessAsync(delivery, owner, cancellationToken);
        }

        return count;
    }

    private async Task SuppressInvalidAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        await dbContext.EmailDeliveries
            .Where(delivery => delivery.Status <= EmailDeliveryStatus.RetryPending
                && (delivery.EnvelopeExpiresAt <= now
                    || !dbContext.AccountTokens.Any(token => token.Id == delivery.AccountTokenId
                        && token.UserId == delivery.UserId && token.Purpose == delivery.Purpose
                        && token.ConsumedAt == null && token.ExpiresAt > now
                        && token.TargetValue == delivery.Recipient
                        && (token.User.Status == UserStatus.Active || token.User.Status == UserStatus.PendingVerification)
                        && token.User.Emails.Any(email => email.IsPrimary && email.Email == delivery.Recipient
                            && (token.Purpose != AccountTokenPurpose.VerifyEmail || email.VerifiedAt == null)))
                    || !dbContext.AccountTokenPolicies.Any(policy => policy.UserId == delivery.UserId
                        && policy.Purpose == delivery.Purpose && policy.ActiveTokenId == delivery.AccountTokenId)))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Status, EmailDeliveryStatus.Suppressed)
                .SetProperty(item => item.ProtectedEnvelope, (string?)null)
                .SetProperty(item => item.LeaseOwner, (Guid?)null)
                .SetProperty(item => item.LeaseUntil, (DateTimeOffset?)null)
                .SetProperty(item => item.TerminalAt, now)
                .SetProperty(item => item.LastErrorCode, "Email.TokenUnavailable"), cancellationToken);

        await dbContext.OutboxEvents.Where(item => item.PublishedAt == null
                && dbContext.EmailDeliveries.Any(delivery => delivery.OutboxEventId == item.Id
                    && delivery.Status >= EmailDeliveryStatus.ProviderAccepted))
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.PublishedAt, now), cancellationToken);
    }

    private async Task ProcessAsync(EmailDelivery delivery, Guid owner, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (delivery.AttemptCount >= _options.MaxAttempts)
        {
            await CompleteAsync(delivery, owner, new EmailSendResult(false, false, "Email.AttemptsExhausted"), cancellationToken);
            return;
        }

        // Recheck immediately before decrypting/sending. Resend may still win after this
        // check: an email already in transit cannot be recalled, but its link is invalid.
        await SuppressInvalidAsync(cancellationToken);
        if (!await Owned(delivery.Id, owner).AnyAsync(cancellationToken)) return;
        string rawToken;
        try
        {
            rawToken = queue.Unprotect(delivery.ProtectedEnvelope!);
        }
        catch (CryptographicException)
        {
            await CompleteAsync(delivery, owner, new EmailSendResult(false, false, "Email.EnvelopeUnavailable"), cancellationToken);
            return;
        }

        var route = delivery.Purpose == AccountTokenPurpose.VerifyEmail ? "verify" : "reset";
        var link = $"{_options.PublicOrigin.TrimEnd('/')}/auth/{route}#token={Uri.EscapeDataString(rawToken)}";
        var verify = delivery.Purpose == AccountTokenPurpose.VerifyEmail;
        var email = new AccountEmail(delivery.Id, delivery.Recipient,
            verify ? "Xác minh email SCDC" : "Đặt lại mật khẩu SCDC",
            $"{(verify ? "Xác minh địa chỉ email của bạn" : "Đặt lại mật khẩu của bạn")} bằng liên kết sau:\r\n\r\n{link}\r\n\r\n"
            + $"Liên kết dùng một lần, hết hạn lúc {delivery.EnvelopeExpiresAt:O} (UTC).\r\n"
            + "Nếu bạn không yêu cầu thao tác này, hãy bỏ qua email.\r\nSCDC");

        if (await Owned(delivery.Id, owner).ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.AttemptCount, item => item.AttemptCount + 1), cancellationToken) == 0) return;
        delivery.AttemptCount++;

        EmailSendResult result;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.SendTimeoutSeconds));
        try
        {
            result = await sender.SendAsync(email, timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            result = new EmailSendResult(false, true, "Email.Timeout");
        }
        catch (Exception exception) when (exception is IOException or System.Net.Sockets.SocketException)
        {
            result = new EmailSendResult(false, true, "Email.TransportUnavailable");
        }

        await CompleteAsync(delivery, owner, result, cancellationToken);
    }

    private IQueryable<EmailDelivery> Owned(Guid id, Guid owner) => dbContext.EmailDeliveries
        .Where(item => item.Id == id && item.Status == EmailDeliveryStatus.Sending && item.LeaseOwner == owner);

    private async Task CompleteAsync(EmailDelivery delivery, Guid owner, EmailSendResult result, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        int[] delays = [10, 30, 90, 300];
        var retryAt = now.AddSeconds(delays[Math.Clamp(delivery.AttemptCount - 1, 0, delays.Length - 1)]
            + Random.Shared.Next(0, 4));
        var retry = !result.Accepted && result.Retryable && delivery.AttemptCount < _options.MaxAttempts
            && retryAt < delivery.EnvelopeExpiresAt;
        var status = result.Accepted ? EmailDeliveryStatus.ProviderAccepted
            : retry ? EmailDeliveryStatus.RetryPending : EmailDeliveryStatus.Failed;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var updated = await Owned(delivery.Id, owner).ExecuteUpdateAsync(setters => setters
            .SetProperty(item => item.Status, status)
            .SetProperty(item => item.ProtectedEnvelope, retry ? delivery.ProtectedEnvelope : null)
            .SetProperty(item => item.LeaseOwner, (Guid?)null)
            .SetProperty(item => item.LeaseUntil, (DateTimeOffset?)null)
            .SetProperty(item => item.NextAttemptAt, retryAt)
            .SetProperty(item => item.TerminalAt, retry ? (DateTimeOffset?)null : now)
            .SetProperty(item => item.ProviderMessageId, result.MessageId)
            .SetProperty(item => item.LastErrorCode, result.ErrorCode), cancellationToken);
        if (updated != 0)
        {
            await dbContext.OutboxEvents.Where(item => item.Id == delivery.OutboxEventId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.PublishedAt, retry ? (DateTimeOffset?)null : now)
                    .SetProperty(item => item.AvailableAt, retryAt)
                    .SetProperty(item => item.AttemptCount, delivery.AttemptCount)
                    .SetProperty(item => item.LastError, result.ErrorCode), cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }
}
