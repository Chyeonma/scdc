using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Infrastructure.Access;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Realtime;

internal sealed class ConversationAccess(MessagingDbContext db, MessagingSession session,
    MessagingIdentityAccess access, MessagingProfiles profiles, TimeProvider clock)
{
    public Task<Result> AuthorizeAsync(AuthenticatedSession actor, Guid space, CancellationToken token) =>
        DeliverAsync(actor, space, null, (_, _) => Task.CompletedTask, token);

    public async Task<Result> DeliverAsync(AuthenticatedSession actor, Guid spaceId, Guid? messageId,
        Func<MessageResponse?, CancellationToken, Task> deliver, CancellationToken token)
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
                    if (!await CanReadAsync(actor.UserId, spaceId, token)) return Missing();
                    if (messageId is Guid id)
                    {
                        var hint = await db.Messages.AsNoTracking().Where(m => m.Id == id && m.SpaceId == spaceId).Select(m => (Guid?)m.AuthorUserId).SingleOrDefaultAsync(token);
                        if (hint is null) return Missing(); author = hint;
                    }
                    await access.EnsureAsync(db, permit, token); await owner.CommitAsync(token);
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
                    if (!await CanReadAsync(actor.UserId, spaceId, token)) return Missing();
                    var message = await db.Messages.AsNoTracking().SingleOrDefaultAsync(m => m.Id == messageId && m.SpaceId == spaceId, token);
                    if (message is null) return Missing();
                    if (!people.TryGetValue(message.AuthorUserId, out var person)) return Unavailable();
                    var response = new MessageResponse(message.Id, message.SpaceId, person, message.ClientMessageId,
                        message.ConversationSequence.ToString(CultureInfo.InvariantCulture), message.Version.ToString(CultureInfo.InvariantCulture),
                        message.DeletedAt is null ? message.Content : null, message.CreatedAt, message.EditedAt, message.DeletedAt);
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
    private async Task<bool> CanReadAsync(Guid actor, Guid spaceId, CancellationToken token)
    {
        var pair = await db.DirectConversations.AsNoTracking().SingleOrDefaultAsync(p => p.SpaceId == spaceId, token);
        if (pair is null || pair.UserLowId != actor && pair.UserHighId != actor) return false;
        await db.Database.SqlQuery<Guid>($"SELECT id AS \"Value\" FROM messaging.spaces WHERE id={spaceId} FOR SHARE").ToListAsync(token);
        if (!await db.Spaces.AsNoTracking().AnyAsync(s => s.Id == spaceId && s.SpaceType == 1 && s.Status != 3 && s.DeletedAt == null, token)) return false;
        var members = await db.SpaceMembers.AsNoTracking().Where(m => m.SpaceId == spaceId && m.MembershipStatus == 1 && m.LeftAt == null).Select(m => m.UserId).ToListAsync(token);
        return members.Count == 2 && members.Contains(pair.UserLowId) && members.Contains(pair.UserHighId);
    }
    private static Result Denied() => Result.Failure(Error.Unauthorized("Common.Unauthorized", "The current session is not available."));
    private static Result Missing() => Result.Failure(Error.NotFound("RESOURCE_NOT_FOUND", "The requested conversation is not available."));
    private static Result Unavailable() => Result.Failure(Error.ServiceUnavailable("AUTHORITY_UNAVAILABLE", "The service cannot confirm access."));
}
