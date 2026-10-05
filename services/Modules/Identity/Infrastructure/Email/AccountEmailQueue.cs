using Microsoft.AspNetCore.DataProtection;
using SCDC.Modules.Identity.Domain;
using SCDC.Modules.Identity.Infrastructure.Persistence;

namespace SCDC.Modules.Identity.Infrastructure.Email;

internal sealed class AccountEmailQueue(IdentityDbContext dbContext, IDataProtectionProvider protectionProvider)
{
    private readonly IDataProtector _protector = protectionProvider.CreateProtector("Identity.EmailDelivery.v1");

    // The caller saves token, delivery, audit and outbox in one transaction.
    public void Enqueue(User user, AccountToken token, string rawToken, DateTimeOffset now)
    {
        var deliveryId = Guid.CreateVersion7();
        var outbox = IdentityData.Outbox(
            token.Purpose == AccountTokenPurpose.VerifyEmail
                ? "Identity.EmailVerificationRequested" : "Identity.PasswordResetRequested",
            user.Id, user.Version, now,
            new { user_id = user.Id, delivery_id = deliveryId, purpose = (short)token.Purpose });
        dbContext.OutboxEvents.Add(outbox);
        dbContext.EmailDeliveries.Add(new EmailDelivery
        {
            Id = deliveryId,
            UserId = user.Id,
            AccountTokenId = token.Id,
            OutboxEventId = outbox.Id,
            Purpose = token.Purpose,
            Recipient = token.TargetValue!,
            ProtectedEnvelope = _protector.Protect(rawToken),
            EnvelopeExpiresAt = token.ExpiresAt,
            NextAttemptAt = now,
            CreatedAt = now
        });
    }

    public string Unprotect(string envelope) => _protector.Unprotect(envelope);
}
