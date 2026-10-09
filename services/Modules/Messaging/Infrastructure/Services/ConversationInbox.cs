using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.BuildingBlocks.Infrastructure;
using SCDC.Contracts.Identity;
using SCDC.Contracts.Persistence;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class ConversationInbox(MessagingDbContext db, ISharedDatabaseSession session,
    IAccountAccessGuard guard, IHistoricalUserSummaryReader summaries, IDataProtectionProvider protection,
    TimeProvider clock) : IConversationInbox
{
    private readonly IDataProtector _protector = protection.CreateProtector("Messaging.Conversations.v1");
    private sealed record Position(DateTimeOffset? Activity, Guid Id);
    private sealed record Cursor(int Version, Guid ActorId, int Limit, string Scope, Position Position, DateTimeOffset ExpiresAt);

    public async Task<Result<DirectConversationPage>> ListAsync(InboxRequest request, CancellationToken cancellationToken)
    {
        if (request.Limit is < 1 or > 50)
            return Result.Failure<DirectConversationPage>(Error.Validation("Common.ValidationFailed", "Use a page size from 1 to 50."));
        try
        {
            await using var owner = await session.BeginAsync(cancellationToken);
            try
            {
                await db.Database.UseTransactionAsync(session.Transaction, cancellationToken);
                var access = await guard.LockPairAsync(new(request.ActorId, request.SessionId,
                    request.SecurityStamp, request.ActorId), cancellationToken);
                if (access.Failure is not null)
                    return Result.Failure<DirectConversationPage>(Error.Unauthorized("Common.Unauthorized", "The current session is not available."));
                var now = clock.GetUtcNow();
                Cursor? cursor = null;
                if (request.Cursor is not null)
                {
                    if (request.Cursor.Length is < 1 or > 4096) return BadCursor();
                    try { cursor = JsonSerializer.Deserialize<Cursor>(_protector.Unprotect(request.Cursor)); }
                    catch (Exception exception) when (exception is CryptographicException or JsonException or ArgumentException) { return BadCursor(); }
                    if (cursor is null || cursor.Version != 1 || cursor.ActorId != request.ActorId
                        || cursor.Limit != request.Limit || cursor.Scope != "direct-inbox" || cursor.ExpiresAt <= now
                        || cursor.Position is null || cursor.Position.Id == Guid.Empty) return BadCursor();
                }
                var query = from member in db.SpaceMembers.AsNoTracking()
                    join space in db.Spaces.AsNoTracking() on member.SpaceId equals space.Id
                    join pair in db.DirectConversations.AsNoTracking() on space.Id equals pair.SpaceId
                    where member.UserId == request.ActorId && member.MembershipStatus == 1 && member.LeftAt == null
                        && space.SpaceType == 1 && space.Status != 3 && space.DeletedAt == null
                        && (pair.UserLowId == request.ActorId || pair.UserHighId == request.ActorId)
                    select new { Space = space, Pair = pair };
                if (cursor is not null)
                {
                    var position = cursor.Position;
                    query = position.Activity is null
                        ? query.Where(x => x.Space.LastActivityAt == null && x.Space.Id.CompareTo(position.Id) > 0)
                        : query.Where(x => x.Space.LastActivityAt == null || x.Space.LastActivityAt < position.Activity
                            || (x.Space.LastActivityAt == position.Activity && x.Space.Id.CompareTo(position.Id) > 0));
                }
                var rows = await query.OrderByDescending(x => x.Space.LastActivityAt != null)
                    .ThenByDescending(x => x.Space.LastActivityAt).ThenBy(x => x.Space.Id)
                    .Take(request.Limit + 1).ToListAsync(cancellationToken);
                var page = rows.Take(request.Limit).ToArray();
                // Deferred membership changes and space deletion serialize on these same space locks.
                if (page.Length > 0)
                {
                    var ids = page.Select(x => x.Space.Id).ToArray();
                    await db.Database.SqlQueryRaw<Guid>("SELECT id AS \"Value\" FROM messaging.spaces WHERE id=ANY(@ids) ORDER BY id FOR SHARE",
                        new NpgsqlParameter("ids", ids)).ToListAsync(cancellationToken);
                    var stillVisible = await db.SpaceMembers.AsNoTracking().Where(m => ids.Contains(m.SpaceId)
                        && m.UserId == request.ActorId && m.MembershipStatus == 1 && m.LeftAt == null)
                        .Join(db.Spaces.Where(s => s.Status != 3 && s.DeletedAt == null), m => m.SpaceId, s => s.Id, (m, s) => s.Id)
                        .ToListAsync(cancellationToken);
                    page = page.Where(x => stillVisible.Contains(x.Space.Id)).ToArray();
                }
                var people = await summaries.ReadAsync(page.SelectMany(x => new[] { x.Pair.UserLowId, x.Pair.UserHighId }).Distinct().ToArray(), cancellationToken);
                var items = new List<DirectConversationResponse>();
                foreach (var row in page)
                {
                    if (!people.TryGetValue(row.Pair.UserLowId, out var low) || !people.TryGetValue(row.Pair.UserHighId, out var high))
                        return Unavailable();
                    items.Add(new(row.Space.Id, new[] { low, high }, row.Space.CreatedAt,
                        (row.Space.LastMessageSequence ?? 0).ToString(CultureInfo.InvariantCulture), row.Space.LastActivityAt));
                }
                string? next = null;
                if (rows.Count > request.Limit)
                {
                    var last = rows[request.Limit - 1].Space;
                    next = _protector.Protect(JsonSerializer.Serialize(new Cursor(1, request.ActorId, request.Limit,
                        "direct-inbox", new(last.LastActivityAt, last.Id), cursor?.ExpiresAt ?? now.AddHours(24))));
                }
                await owner.CommitAsync(cancellationToken);
                return Result.Success(new DirectConversationPage(items, next));
            }
            finally { await db.Database.UseTransactionAsync(null, CancellationToken.None); await guard.DetachAsync(default); }
        }
        catch (Exception exception) when (DatabaseAvailability.IsUnavailable(exception)) { return Unavailable(); }
    }
    private static Result<DirectConversationPage> BadCursor() => Result.Failure<DirectConversationPage>(
        Error.Validation("CURSOR_INVALID", "The inbox cursor is invalid or expired; restart the list."));
    private static Result<DirectConversationPage> Unavailable() => Result.Failure<DirectConversationPage>(
        Error.ServiceUnavailable("AUTHORITY_UNAVAILABLE", "The service cannot confirm this request. Please try again."));
}
