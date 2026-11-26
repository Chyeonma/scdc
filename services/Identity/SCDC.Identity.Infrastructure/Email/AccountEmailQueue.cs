using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using SCDC.Modules.Identity.Domain;
using SCDC.Modules.Identity.Infrastructure.Persistence;

namespace SCDC.Modules.Identity.Infrastructure.Email;

internal sealed class AccountEmailQueue(IdentityDbContext dbContext, [FromKeyedServices("Identity.Email")] IDataProtectionProvider protectionProvider)
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
        var delivery = new EmailDelivery
        {
            Id = deliveryId,
            UserId = user.Id,
            AccountTokenId = token.Id,
            OutboxEventId = outbox.Id,
            Purpose = token.Purpose,
            Recipient = token.TargetValue!,
            EnvelopeExpiresAt = token.ExpiresAt,
            NextAttemptAt = now,
            CreatedAt = now
        };
        delivery.ProtectedEnvelope = Protector(delivery).Protect(rawToken);
        dbContext.EmailDeliveries.Add(delivery);
    }

    private IDataProtector Protector(EmailDelivery delivery) => _protector.CreateProtector(FormattableString.Invariant(
        $"{delivery.Id:N}:{delivery.UserId:N}:{delivery.AccountTokenId:N}:{(short)delivery.Purpose}:{delivery.TemplateVersion}:{delivery.Recipient}"));

    public string Unprotect(EmailDelivery delivery) => Protector(delivery).Unprotect(delivery.ProtectedEnvelope!);
}
