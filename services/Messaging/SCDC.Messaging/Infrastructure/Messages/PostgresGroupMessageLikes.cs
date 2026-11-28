using Microsoft.EntityFrameworkCore;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Domain;
using SCDC.Modules.Messaging.Infrastructure.Access;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Messages;

internal sealed class PostgresGroupMessageLikes(MessagingDbContext db, MessagingSession session,
    MessagingIdentityAccess identity, PrivateConversationMembership membership) : IGroupMessageLikes
{
    private const string Like = "👍";
    internal static readonly MessageLikes None = new(0, false);

    public Task<Result<MessageLikes>> ReadAsync(GroupMessageLikeRequest request, CancellationToken token) =>
        RunAsync(request, null, token);

    public Task<Result<MessageLikes>> SetAsync(GroupMessageLikeRequest request, bool liked, CancellationToken token) =>
        RunAsync(request, liked, token);

    private async Task<Result<MessageLikes>> RunAsync(GroupMessageLikeRequest request, bool? liked, CancellationToken token)
    {
        try
        {
            var permit = await identity.AcquireAsync(request.Actor, IdentityPurpose.DmRead, request.ConversationId, token);
            await using var owner = await session.BeginAsync(token);
            try
            {
                await db.Database.UseTransactionAsync(session.Transaction, token);
                await identity.AdmitAsync(db, permit, token);
                var actor = request.Actor.UserId;
                var message = request.MessageId;
                var space = await membership.LockAsync(actor, request.ConversationId, ConversationKind.Group, liked.HasValue, token);
                if (space is null || !await db.Messages.AnyAsync(m => m.Id == message && m.SpaceId == request.ConversationId
                    && m.DeletedAt == null, token))
                    return Result.Failure<MessageLikes>(Error.NotFound("RESOURCE_NOT_FOUND", "The requested message is not available."));
                if (liked is true)
                    await db.Database.ExecuteSqlInterpolatedAsync($"""
                        INSERT INTO messaging.reactions (message_id, user_id, reaction_key) VALUES ({message}, {actor}, {Like})
                        ON CONFLICT (message_id, user_id, reaction_key) DO NOTHING
                        """, token);
                else if (liked is false)
                    await db.Database.ExecuteSqlInterpolatedAsync($"""
                        DELETE FROM messaging.reactions WHERE message_id={message} AND user_id={actor} AND reaction_key={Like}
                        """, token);

                var counts = await ReadCountsAsync(db, [message], actor, token);
                await identity.EnsureAsync(db, permit, token);
                await owner.CommitAsync(token);
                return Result.Success(counts.GetValueOrDefault(message, None));
            }
            finally { await db.Database.UseTransactionAsync(null, CancellationToken.None); }
        }
        catch (MessagingAccessFailure e) { return Result.Failure<MessageLikes>(e.Error); }
        catch (Exception e) when (MessagingFailures.Temporary(e))
        {
            return Result.Failure<MessageLikes>(Error.ServiceUnavailable("AUTHORITY_UNAVAILABLE", "The service cannot confirm this request."));
        }
    }

    internal static async Task<Dictionary<Guid, MessageLikes>> ReadCountsAsync(MessagingDbContext db, Guid[] ids,
        Guid actor, CancellationToken token)
    {
        if (ids.Length == 0) return [];
        var rows = await db.Database.SqlQuery<LikeRow>($"""
            SELECT message_id AS "MessageId", count(*)::integer AS "Count", bool_or(user_id = {actor}) AS "LikedByMe"
            FROM messaging.reactions WHERE reaction_key={Like} AND message_id=ANY({ids}) GROUP BY message_id
            """).ToListAsync(token);
        return rows.ToDictionary(row => row.MessageId, row => new MessageLikes(row.Count, row.LikedByMe));
    }

    private sealed record LikeRow(Guid MessageId, int Count, bool LikedByMe);
}
