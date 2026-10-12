using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.BuildingBlocks.Infrastructure;
using SCDC.Contracts.Identity;
using SCDC.Contracts.Persistence;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Realtime;

internal sealed class ConversationAccess(MessagingDbContext db, ISharedDatabaseSession session,
    IAccountAccessGuard guard, IHistoricalUserSummaryReader summaries, TimeProvider clock)
{
    public Task<Result> AuthorizeAsync(AuthenticatedSession actor, Guid space, CancellationToken token) =>
        DeliverAsync(actor, space, null, _ => Task.CompletedTask, token);

    // Hold Identity and space SHARE locks through delivery: a revoke/unsubscribe ack cannot
    // race a later authorization based on a stale group membership or cached user summary.
    public async Task<Result> DeliverAsync(AuthenticatedSession actor, Guid spaceId, Guid? messageId,
        Func<MessageResponse?, Task> deliver, CancellationToken token)
    {
        if (actor.AccessExpiresAt <= clock.GetUtcNow()) return Denied();
        try
        {
            await using var owner = await session.BeginAsync(token);
            try
            {
                await db.Database.UseTransactionAsync(session.Transaction, token);
                var access = await guard.LockPairAsync(new(actor.UserId, actor.SessionId, actor.SecurityStamp, actor.UserId), token);
                if (access.Failure != null) return Denied();
                var pair = await db.DirectConversations.AsNoTracking().SingleOrDefaultAsync(p => p.SpaceId == spaceId, token);
                if (pair is null || pair.UserLowId != actor.UserId && pair.UserHighId != actor.UserId) return Missing();
                await db.Database.SqlQuery<Guid>($"SELECT id AS \"Value\" FROM messaging.spaces WHERE id={spaceId} FOR SHARE").ToListAsync(token);
                var space = await db.Spaces.AsNoTracking().AnyAsync(s => s.Id == spaceId && s.SpaceType == 1 && s.Status != 3 && s.DeletedAt == null, token);
                var members = await db.SpaceMembers.AsNoTracking().Where(m => m.SpaceId == spaceId && m.MembershipStatus == 1 && m.LeftAt == null)
                    .Select(m => m.UserId).ToListAsync(token);
                if (!space || members.Count != 2 || !members.Contains(pair.UserLowId) || !members.Contains(pair.UserHighId)) return Missing();
                MessageResponse? response = null;
                if (messageId is not null)
                {
                    var m = await db.Messages.AsNoTracking().SingleOrDefaultAsync(m => m.Id == messageId && m.SpaceId == spaceId, token);
                    if (m is null) return Missing();
                    var people = await summaries.ReadAsync(new[] { m.AuthorUserId }, token);
                    if (!people.TryGetValue(m.AuthorUserId, out var author)) return Unavailable();
                    response = new(m.Id, m.SpaceId, author, m.ClientMessageId,
                        m.ConversationSequence.ToString(CultureInfo.InvariantCulture), m.Version.ToString(CultureInfo.InvariantCulture),
                        m.DeletedAt is null ? m.Content : null, m.CreatedAt, m.EditedAt, m.DeletedAt);
                }
                if (actor.AccessExpiresAt <= clock.GetUtcNow()) return Denied();
                await deliver(response);
                await owner.CommitAsync(token);
                return Result.Success();
            }
            finally { await db.Database.UseTransactionAsync(null, CancellationToken.None); await guard.DetachAsync(CancellationToken.None); }
        }
        catch (Exception exception) when (DatabaseAvailability.IsUnavailable(exception)) { return Unavailable(); }
    }
    private static Result Denied() => Result.Failure(Error.Unauthorized("Common.Unauthorized", "The current session is not available."));
    private static Result Missing() => Result.Failure(Error.NotFound("RESOURCE_NOT_FOUND", "The requested conversation is not available."));
    private static Result Unavailable() => Result.Failure(Error.ServiceUnavailable("AUTHORITY_UNAVAILABLE", "The service cannot confirm access."));
}
