using Microsoft.EntityFrameworkCore;
using SCDC.Contracts.Identity;
using SCDC.Contracts.Persistence;
using SCDC.Modules.Identity.Domain;
using SCDC.Modules.Identity.Infrastructure.Persistence;

namespace SCDC.Modules.Identity.Infrastructure.Services;

internal sealed class AccountAccessGuard(IdentityDbContext db, ISharedDatabaseSession session,
    IHistoricalUserSummaryReader summaries, TimeProvider clock) : IAccountAccessGuard
{
    public async Task DetachAsync(CancellationToken cancellationToken) =>
        await db.Database.UseTransactionAsync(null, cancellationToken);

    public async Task<AccountAccessResult> LockPairAsync(AccountAccessRequest request, CancellationToken cancellationToken)
    {
        if (session.Transaction is null) throw new InvalidOperationException("The account guard requires a shared transaction.");
        await db.Database.UseTransactionAsync(session.Transaction, cancellationToken);
        var pair = UuidNetworkOrder.Pair(request.ActorId, request.PeerId);
        foreach (var id in new[] { pair.Low, pair.High }.Distinct())
            await db.Database.SqlQuery<Guid>($"""
                SELECT id AS "Value" FROM identity.users WHERE id = {id} FOR SHARE
                """).ToListAsync(cancellationToken);
        await db.Database.SqlQuery<Guid>($"""
            SELECT id AS "Value" FROM identity.auth_sessions
            WHERE id = {request.SessionId} AND user_id = {request.ActorId} FOR SHARE
            """).ToListAsync(cancellationToken);

        var now = clock.GetUtcNow();
        var actorValid = await db.AuthSessions.AsNoTracking().AnyAsync(s =>
            s.Id == request.SessionId && s.UserId == request.ActorId && s.RevokedAt == null && s.ExpiresAt > now
            && s.User.Status == UserStatus.Active && s.User.DeletedAt == null
            && s.User.SecurityState != null && s.User.SecurityState.SecurityStamp == request.SecurityStamp
            && s.User.Emails.Any(e => e.IsPrimary && e.VerifiedAt != null), cancellationToken);
        if (!actorValid) return new(null, null, false, new("Common.Unauthorized"));
        var people = await summaries.ReadAsync(new[] { request.ActorId, request.PeerId }, cancellationToken);
        if (!people.TryGetValue(request.ActorId, out var actor)) return new(null, null, false, new("Common.Unauthorized"));
        people.TryGetValue(request.PeerId, out var peer);
        return new(actor, peer, peer is not null && peer.Availability is null, null);
    }
}
