using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.BuildingBlocks.Infrastructure;
using SCDC.Contracts.Identity;
using SCDC.Contracts.Persistence;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class MessageHistory(MessagingDbContext db, ISharedDatabaseSession session,
    IAccountAccessGuard guard, IHistoricalUserSummaryReader summaries, IDataProtectionProvider protection,
    TimeProvider clock) : IMessageHistory
{
    private readonly IDataProtector protector = protection.CreateProtector("Messaging.History.v1");
    private sealed record Cursor(int Version, Guid ActorId, Guid ConversationId, int Limit, string Scope,
        string Mode, long Position, long Frontier, DateTimeOffset ExpiresAt);

    public async Task<Result<MessagePage>> ReadAsync(HistoryRequest request, CancellationToken cancellationToken)
    {
        if (request.Limit is < 1 or > 100 || request.Before is not null && request.After is not null
            || request.Through is not null && (request.After is null || request.After == "0"))
            return Fail(Error.Validation("Common.ValidationFailed", "Use limit 1–100 and one history direction; through requires a protected after cursor."));
        try
        {
            await using var owner = await session.BeginAsync(cancellationToken);
            try
            {
                await db.Database.UseTransactionAsync(session.Transaction, cancellationToken);
                // Read access only requires the actor to be eligible. An unavailable peer keeps their history.
                var access = await guard.LockPairAsync(new(request.ActorId, request.SessionId,
                    request.SecurityStamp, request.ActorId), cancellationToken);
                if (access.Failure is not null) return Fail(Error.Unauthorized("Common.Unauthorized", "The current session is not available."));
                var pair = await db.DirectConversations.AsNoTracking().SingleOrDefaultAsync(p => p.SpaceId == request.ConversationId, cancellationToken);
                if (pair is null || pair.UserLowId != request.ActorId && pair.UserHighId != request.ActorId) return NotFound();
                // Writers hold FOR UPDATE until commit. This lock waits for a held writer before reading H,
                // and prevents a lower sequence from committing after the reader has sealed its frontier.
                await db.Database.SqlQuery<Guid>($"SELECT id AS \"Value\" FROM messaging.spaces WHERE id={request.ConversationId} FOR SHARE")
                    .ToListAsync(cancellationToken);
                var space = await db.Spaces.AsNoTracking().SingleOrDefaultAsync(s => s.Id == request.ConversationId
                    && s.SpaceType == 1 && s.Status != 3 && s.DeletedAt == null, cancellationToken);
                var members = await db.SpaceMembers.AsNoTracking().Where(m => m.SpaceId == request.ConversationId
                    && m.MembershipStatus == 1 && m.LeftAt == null).Select(m => m.UserId).ToListAsync(cancellationToken);
                if (space is null || members.Count != 2 || !members.Contains(pair.UserLowId) || !members.Contains(pair.UserHighId)) return NotFound();
                var now = clock.GetUtcNow();
                var raw = request.Before ?? request.After;
                Cursor? cursor = null;
                if (raw is not null && !(request.After == "0" && request.Before is null))
                {
                    if (raw.Length is < 1 or > 4096) return BadCursor();
                    try { cursor = JsonSerializer.Deserialize<Cursor>(protector.Unprotect(raw)); }
                    catch (Exception exception) when (exception is CryptographicException or JsonException or ArgumentException) { return BadCursor(); }
                    if (cursor is null || cursor.Version != 1 || cursor.ActorId != request.ActorId || cursor.ConversationId != request.ConversationId
                        || cursor.Limit != request.Limit || cursor.Scope != "direct-messages" || cursor.ExpiresAt <= now
                        || cursor.Frontier < 0 || cursor.Position < 0 || cursor.Position > cursor.Frontier
                        || (request.Before is not null ? cursor.Mode != "before" : cursor.Mode is not ("after" or "resume"))) return BadCursor();
                    if (request.Through is not null && (!long.TryParse(request.Through, NumberStyles.None, CultureInfo.InvariantCulture, out var through)
                        || through != cursor.Frontier)) return BadCursor();
                }
                var forward = request.After is not null;
                var frontier = cursor is { Mode: "before" or "after" } ? cursor.Frontier : space.LastMessageSequence ?? 0;
                var position = cursor?.Position ?? 0;
                var expires = cursor?.ExpiresAt ?? now.AddHours(24);
                var query = db.Messages.AsNoTracking().Where(m => m.SpaceId == request.ConversationId && m.ConversationSequence <= frontier);
                if (forward) query = query.Where(m => m.ConversationSequence > position);
                else if (cursor is not null) query = query.Where(m => m.ConversationSequence < position);
                query = forward ? query.OrderBy(m => m.ConversationSequence) : query.OrderByDescending(m => m.ConversationSequence);
                // Nullable projections fail closed for incompatible legacy rows rather than silently skipping them.
                var rows = await query.Take(request.Limit + 1).Select(m => new { m.Id, m.SpaceId,
                    AuthorId = (Guid?)m.AuthorUserId, ClientId = (Guid?)m.ClientMessageId,
                    m.ConversationSequence, m.Version, m.Content, m.CreatedAt, m.EditedAt, m.DeletedAt }).ToListAsync(cancellationToken);
                var page = rows.Take(request.Limit).OrderBy(m => m.ConversationSequence).ToArray();
                if (page.Any(m => m.AuthorId is null || m.ClientId is null)) return Unavailable();
                var people = await summaries.ReadAsync(page.Select(m => m.AuthorId!.Value).Distinct().ToArray(), cancellationToken);
                if (page.Any(m => !people.ContainsKey(m.AuthorId!.Value))) return Unavailable();
                var items = page.Select(m => new MessageResponse(m.Id, m.SpaceId, people[m.AuthorId!.Value], m.ClientId!.Value,
                    m.ConversationSequence.ToString(CultureInfo.InvariantCulture), m.Version.ToString(CultureInfo.InvariantCulture),
                    m.DeletedAt is null ? m.Content : null, m.CreatedAt, m.EditedAt, m.DeletedAt)).ToArray();
                string Protect(string mode, long pos) => protector.Protect(JsonSerializer.Serialize(new Cursor(1, request.ActorId,
                    request.ConversationId, request.Limit, "direct-messages", mode, pos, frontier, expires)));
                var more = rows.Count > request.Limit;
                var next = more ? Protect(forward ? "after" : "before", forward ? page[^1].ConversationSequence : page[0].ConversationSequence) : null;
                var resume = request.Before is null && (!forward || !more) ? Protect("resume", frontier) : null;
                await owner.CommitAsync(cancellationToken);
                return Result.Success(new MessagePage(items, next, more, frontier.ToString(CultureInfo.InvariantCulture), resume));
            }
            finally { await db.Database.UseTransactionAsync(null, CancellationToken.None); await guard.DetachAsync(default); }
        }
        catch (Exception exception) when (DatabaseAvailability.IsUnavailable(exception)) { return Unavailable(); }
    }
    private static Result<MessagePage> Fail(Error error) => Result.Failure<MessagePage>(error);
    private static Result<MessagePage> BadCursor() => Fail(Error.Validation("CURSOR_INVALID", "The history cursor is invalid or expired; reload history."));
    private static Result<MessagePage> NotFound() => Fail(Error.NotFound("RESOURCE_NOT_FOUND", "The requested conversation is not available."));
    private static Result<MessagePage> Unavailable() => Fail(Error.ServiceUnavailable("AUTHORITY_UNAVAILABLE", "The service cannot confirm this request. Please try again."));
}
