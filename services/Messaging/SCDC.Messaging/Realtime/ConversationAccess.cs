using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Domain;
using SCDC.Modules.Messaging.Infrastructure.Access;
using SCDC.Modules.Messaging.Infrastructure.Messages;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Realtime;

internal sealed class ConversationAccess(MessagingDbContext db, MessagingSession session,
    MessagingIdentityAccess access, MessagingProfiles profiles, PrivateConversationMembership membership, TimeProvider clock)
{
    public Task<Result> AuthorizeAsync(AuthenticatedSession actor, Guid space, CancellationToken token, ConversationKind? kind = null) =>
        DeliverAsync(actor, space, null, (_, _) => Task.CompletedTask, token, kind);

    public async Task<Result> DeliverAsync(AuthenticatedSession actor, Guid spaceId, Guid? messageId,
        Func<MessageResponse?, CancellationToken, Task> deliver, CancellationToken token, ConversationKind? kind = null)
    {
        if (actor.AccessExpiresAt <= clock.GetUtcNow()) return Denied();
        try
        {
            var permit = await access.AcquireAsync(actor, IdentityPurpose.DmRead, spaceId, token);
            Guid? author = null;
            await using (var owner = await session.BeginAsync(token))
            {
                try
                {
                    await db.Database.UseTransactionAsync(session.Transaction, token);
                    await access.AdmitAsync(db, permit, token);
                    if (await membership.LockAsync(actor.UserId, spaceId, kind, write: false, token) is null) return Missing();
                    if (messageId is Guid id)
                    {
                        var hint = await db.Messages.AsNoTracking().Where(m => m.Id == id && m.SpaceId == spaceId).Select(m => (Guid?)m.AuthorUserId).SingleOrDefaultAsync(token);
                        if (hint is null) return Missing(); author = hint;
                    }
                    await access.EnsureAsync(db, permit, token);
                    if (messageId is null)
                    {
                        using var delivery = CancellationTokenSource.CreateLinkedTokenSource(token);
                        delivery.CancelAfter(TimeSpan.FromMilliseconds(500));
                        await deliver(null, delivery.Token).WaitAsync(delivery.Token);
                    }
                    await owner.CommitAsync(token);
                }
                finally { await db.Database.UseTransactionAsync(null, CancellationToken.None); }
            }
            if (messageId is null) return Result.Success();
            // No owner transaction/fence remains held during the independent profile request.
            var people = await profiles.ReadAsync(new[] { author!.Value }, token);
            permit = await access.AcquireAsync(actor, IdentityPurpose.DmRead, spaceId, token);
            await using (var owner = await session.BeginAsync(token))
            {
                try
                {
                    await db.Database.UseTransactionAsync(session.Transaction, token);
                    await access.AdmitAsync(db, permit, token);
                    if (await membership.LockAsync(actor.UserId, spaceId, kind, write: false, token) is null) return Missing();
                    var message = await db.Messages.AsNoTracking().SingleOrDefaultAsync(m => m.Id == messageId && m.SpaceId == spaceId, token);
                    if (message is null) return Missing();
                    if (!people.TryGetValue(message.AuthorUserId, out var person)) return Unavailable();
                    var reply = message.DeletedAt is null ? await ReplyPreviews.ReadAsync(db, spaceId, message.ReplyToMessageId, token) : null;
                    var response = new MessageResponse(message.Id, message.SpaceId, person, message.ClientMessageId,
                        message.ConversationSequence.ToString(CultureInfo.InvariantCulture), message.Version.ToString(CultureInfo.InvariantCulture),
                        message.DeletedAt is null ? message.Content : null, message.CreatedAt, message.EditedAt, message.DeletedAt, Reply: reply);
                    await access.EnsureAsync(db, permit, token);
                    // Complete/cancel network handoff before the local one-second transaction budget.
                    using var delivery = CancellationTokenSource.CreateLinkedTokenSource(token); delivery.CancelAfter(TimeSpan.FromMilliseconds(500));
                    await deliver(response, delivery.Token).WaitAsync(delivery.Token);
                    await access.EnsureAsync(db, permit, token); await owner.CommitAsync(token); return Result.Success();
                }
                finally { await db.Database.UseTransactionAsync(null, CancellationToken.None); }
            }
        }
        catch (MessagingAccessFailure e) { return e.Error.Code == "Common.Unauthorized" ? Denied() : e.Error.Code == "RESOURCE_NOT_FOUND" ? Missing() : Unavailable(); }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { return Unavailable(); }
        catch (Exception e) when (MessagingFailures.Temporary(e)) { return Unavailable(); }
    }
    private static Result Denied() => Result.Failure(Error.Unauthorized("Common.Unauthorized", "The current session is not available."));
    private static Result Missing() => Result.Failure(Error.NotFound("RESOURCE_NOT_FOUND", "The requested conversation is not available."));
    private static Result Unavailable() => Result.Failure(Error.ServiceUnavailable("AUTHORITY_UNAVAILABLE", "The service cannot confirm access."));
}
