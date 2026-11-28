using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;
using SCDC.Modules.Messaging.Application.GroupConversations;
using SCDC.Modules.Messaging.Domain;
using SCDC.Modules.Messaging.Infrastructure.Access;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.GroupConversations;

internal sealed class PostgresGroupConversations(MessagingDbContext db, MessagingSession session,
    MessagingIdentityAccess access, TimeProvider clock) : IGroupConversationStore
{
    public Task<Result<GroupSummary>> CreateAsync(AuthenticatedSession actor, Guid id, string name, Guid[] memberIds,
        CancellationToken token) => RunAsync(async () =>
    {
        var replay = await db.GroupConversations.AnyAsync(g => g.SpaceId == id && g.OwnerUserId == actor.UserId, token);
        var permits = new List<IdentityPermit>();
        if (replay)
            permits.Add(await access.AcquireAsync(actor, IdentityPurpose.DmRead, id, token, operation: id));
        else
            // Identity already validates the caller and one peer per DmOpenNew permit.
            foreach (var member in memberIds)
                permits.Add(await access.AcquireAsync(actor, IdentityPurpose.DmOpenNew, id, token, peer: member, operation: id));

        return await InTransactionAsync(permits, async () =>
        {
            var now = clock.GetUtcNow();
            var inserted = replay ? 0 : await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO messaging.spaces (id, space_type, created_by_user_id, created_at)
                VALUES ({id}, {(short)ConversationKind.Group}, {actor.UserId}, {now})
                ON CONFLICT (id) DO NOTHING
                """, token);
            var members = memberIds.Append(actor.UserId).ToHashSet();
            if (inserted != 0)
            {
                db.GroupConversations.Add(new GroupConversation
                {
                    SpaceId = id, Name = name, OwnerUserId = actor.UserId,
                    MaxMemberCount = GroupConversation.MaxMembers, CreatedAt = now
                });
                db.SpaceMembers.AddRange(members.Select(user => new SpaceMember
                {
                    SpaceId = id, UserId = user, JoinedAt = now, MemberRole = user == actor.UserId ? (short)3 : (short)1
                }));
                await db.SaveChangesAsync(token);
            }

            var current = await MemberGroups(actor.UserId, only: id).SingleOrDefaultAsync(token);
            if (current is null || current.OwnerUserId != actor.UserId)
                return Result.Failure<GroupSummary>(Error.NotFound("RESOURCE_NOT_FOUND", "The requested conversation is not available."));
            var storedMembers = await db.SpaceMembers.Where(m => m.SpaceId == id)
                .Select(m => m.UserId).ToListAsync(token);
            if (current.Name != name || !members.SetEquals(storedMembers))
                return Result.Failure<GroupSummary>(Error.Conflict("OPERATION_CONFLICT", "This ID was used for a different group."));
            return Result.Success(ToSummary(current));
        }, token);
    });

    public Task<Result<GroupPage>> ListAsync(ListGroups query, CancellationToken token) => RunAsync(async () =>
    {
        var permit = await access.AcquireAsync(query.Actor, IdentityPurpose.DmRead, query.Actor.UserId, token);
        return await InTransactionAsync([permit], async () =>
        {
            var rows = await MemberGroups(query.Actor.UserId, after: query.After).Take(query.Limit + 1).ToListAsync(token);
            var items = rows.Take(query.Limit).Select(ToSummary).ToArray();
            return Result.Success(new GroupPage(items, rows.Count > query.Limit ? items[^1].Id : null));
        }, token);
    });

    private IQueryable<GroupRow> MemberGroups(Guid actorId, Guid? only = null, Guid? after = null) =>
        from groupRow in db.GroupConversations.AsNoTracking()
        join space in db.Spaces.AsNoTracking() on groupRow.SpaceId equals space.Id
        join member in db.SpaceMembers.AsNoTracking() on space.Id equals member.SpaceId
        where member.UserId == actorId && member.MembershipStatus == 1 && member.LeftAt == null
            && space.SpaceType == (short)ConversationKind.Group && space.Status != 3 && space.DeletedAt == null
            && (!only.HasValue || space.Id == only.Value) && (!after.HasValue || space.Id.CompareTo(after.Value) > 0)
        orderby space.Id
        select new GroupRow(space.Id, groupRow.Name, groupRow.OwnerUserId,
            db.SpaceMembers.Count(m => m.SpaceId == space.Id && m.MembershipStatus == 1 && m.LeftAt == null),
            space.CreatedAt, space.LastMessageSequence ?? 0, space.LastActivityAt);

    private async Task<Result<T>> InTransactionAsync<T>(IReadOnlyList<IdentityPermit> permits, Func<Task<Result<T>>> work, CancellationToken token)
    {
        await using var transaction = await session.BeginAsync(token);
        try
        {
            await db.Database.UseTransactionAsync(session.Transaction, token);
            await access.AdmitAsync(db, permits, token);
            var result = await work();
            if (result.IsFailure) return result;
            await access.EnsureAsync(db, permits.MinBy(permit => permit.ExpiresAt)!, token);
            await transaction.CommitAsync(token);
            return result;
        }
        finally { await db.Database.UseTransactionAsync(null, CancellationToken.None); }
    }

    private static async Task<Result<T>> RunAsync<T>(Func<Task<Result<T>>> work)
    {
        try { return await work(); }
        catch (MessagingAccessFailure error) { return Result.Failure<T>(error.Error); }
        catch (Exception error) when (MessagingFailures.Temporary(error))
        {
            return Result.Failure<T>(Error.ServiceUnavailable("AUTHORITY_UNAVAILABLE", "The service cannot confirm this request."));
        }
    }

    private static GroupSummary ToSummary(GroupRow row) => new(row.Id, row.Name, row.OwnerUserId, row.MemberCount,
        row.CreatedAt, row.LastSequence.ToString(CultureInfo.InvariantCulture), row.LastActivityAt);

    private sealed record GroupRow(Guid Id, string Name, Guid OwnerUserId, int MemberCount,
        DateTimeOffset CreatedAt, long LastSequence, DateTimeOffset? LastActivityAt);
}
