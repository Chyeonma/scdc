using Microsoft.Extensions.Configuration;
using Npgsql;
using SCDC.Contracts.Identity;

namespace SCDC.Modules.Identity.Infrastructure.Services;

internal sealed class IdentityPermitIssuer(IConfiguration configuration, AccessOwner owner, TimeProvider clock) : IIdentityPermitIssuer
{
    public async Task<IdentityPermit> AcquireAsync(PermitRequest request, CancellationToken ct)
    {
        var dm = request.Purpose is >= IdentityPurpose.DmOpenNew and <= IdentityPurpose.DmDeleteOwn;
        var community = request.Purpose is IdentityPurpose.CommunityWrite or IdentityPurpose.CommunityManage or IdentityPurpose.ChannelCreate;
        var needsPeer = request.Purpose is IdentityPurpose.DmOpenNew or IdentityPurpose.DmSendNew;
        var resuming = request.Context == IdentityAccessContext.ResumingAcceptedOperation;
        if (!Enum.IsDefined(request.Purpose) || request.Actor.UserId == Guid.Empty || request.Actor.SessionId == Guid.Empty
            || (!resuming && request.Actor.SecurityStamp == Guid.Empty) || !Enum.IsDefined(request.Context)
            || (resuming && (owner != AccessOwner.Community || request.Purpose is not (IdentityPurpose.CommunityManage or IdentityPurpose.ChannelCreate)))
            || request.ResourceId == Guid.Empty || request.OperationId == Guid.Empty
            || request.RoutingGeneration != 1 || (dm && owner != AccessOwner.Messaging) || (community && owner != AccessOwner.Community)
            || needsPeer != (request.PeerId is not null) || request.PeerId == Guid.Empty)
            throw new AccessCheckException("INVALID_PERMIT_REQUEST");
        await using var connection = new NpgsqlConnection(configuration.GetConnectionString("Identity"));
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using (var budget = new NpgsqlCommand("SET LOCAL lock_timeout='500ms'; SET LOCAL statement_timeout='2s'", connection, transaction))
            await budget.ExecuteNonQueryAsync(ct);
        // Same account locks as accepted security commands; no permit can slip past an intent.
        foreach (var user in new[] { request.Actor.UserId, request.PeerId ?? request.Actor.UserId }.Distinct().OrderBy(id => id.ToString("D"), StringComparer.Ordinal))
        {
            await using var guard = new NpgsqlCommand("SELECT id FROM identity.users WHERE id=@user FOR SHARE", connection, transaction);
            guard.Parameters.AddWithValue("user", user);
            if (await guard.ExecuteScalarAsync(ct) is null) throw new AccessCheckException("Common.Unauthorized");
        }
        await using (var session = new NpgsqlCommand("""
            SELECT s.expires_at FROM identity.auth_sessions s JOIN identity.users u ON u.id=s.user_id
            JOIN identity.user_security_states security ON security.user_id=u.id
            WHERE s.user_id=@user AND s.id=@session AND s.revoked_at IS NULL AND s.expires_at>clock_timestamp()
            AND u.status=1 AND u.deleted_at IS NULL AND (NOT @require_stamp OR security.security_stamp=@stamp)
            AND EXISTS(SELECT 1 FROM identity.user_emails e WHERE e.user_id=u.id AND e.is_primary AND e.verified_at IS NOT NULL)
            """, connection, transaction))
        {
            session.Parameters.AddWithValue("user", request.Actor.UserId); session.Parameters.AddWithValue("session", request.Actor.SessionId);
            session.Parameters.AddWithValue("stamp", request.Actor.SecurityStamp);
            session.Parameters.AddWithValue("require_stamp", !resuming);
            if (await session.ExecuteScalarAsync(ct) is not DateTime expiry || expiry <= clock.GetUtcNow().UtcDateTime)
                throw new AccessCheckException("Common.Unauthorized");
            if (request.Actor.AccessExpiresAt <= clock.GetUtcNow()) throw new AccessCheckException("Common.Unauthorized");
            if (needsPeer)
            {
                await using var peer = new NpgsqlCommand("""
                    SELECT EXISTS(SELECT 1 FROM identity.users u WHERE u.id=@peer AND u.status=1 AND u.deleted_at IS NULL
                    AND EXISTS(SELECT 1 FROM identity.user_emails e WHERE e.user_id=u.id AND e.is_primary AND e.verified_at IS NOT NULL))
                    """, connection, transaction);
                peer.Parameters.AddWithValue("peer", request.PeerId!.Value);
                if (await peer.ExecuteScalarAsync(ct) is not true) throw new AccessCheckException("RESOURCE_NOT_FOUND");
            }
            var targets = new List<(short Kind, Guid User, Guid Session)> { (1, request.Actor.UserId, Guid.Empty), (2, request.Actor.UserId, request.Actor.SessionId) };
            if (needsPeer) targets.Add((1, request.PeerId!.Value, Guid.Empty));
            var stamps = new List<IdentityScope>();
            foreach (var target in targets.Distinct().OrderBy(t => t.Kind).ThenBy(t => t.User.ToString("D"), StringComparer.Ordinal).ThenBy(t => t.Session.ToString("D"), StringComparer.Ordinal))
            {
                await using var create = new NpgsqlCommand("""
                    INSERT INTO identity.access_scopes(scope_kind,user_id,session_id) VALUES(@kind,@user,@session) ON CONFLICT DO NOTHING
                    """, connection, transaction);
                create.Parameters.AddWithValue("kind", target.Kind); create.Parameters.AddWithValue("user", target.User); create.Parameters.AddWithValue("session", target.Session);
                await create.ExecuteNonQueryAsync(ct);
                await using var read = new NpgsqlCommand("""
                    SELECT current_epoch,pending_operation_id,retired FROM identity.access_scopes
                    WHERE scope_kind=@kind AND user_id=@user AND session_id=@session FOR SHARE
                    """, connection, transaction);
                read.Parameters.AddWithValue("kind", target.Kind); read.Parameters.AddWithValue("user", target.User); read.Parameters.AddWithValue("session", target.Session);
                await using var row = await read.ExecuteReaderAsync(ct); await row.ReadAsync(ct);
                if (!row.IsDBNull(1)) throw new AccessCheckException("ACCESS_PENDING");
                if (row.GetBoolean(2)) throw new AccessCheckException("Common.Unauthorized");
                stamps.Add(new(target.Kind, target.User, target.Session, row.GetInt64(0)));
            }
            await transaction.CommitAsync(ct);
            var now = clock.GetUtcNow();
            var expires = new[] { now.AddSeconds(5), request.Actor.AccessExpiresAt, new DateTimeOffset(expiry, TimeSpan.Zero) }.Min();
            if (expires - now < TimeSpan.FromMilliseconds(1500)) throw new AccessCheckException("ACCESS_CHANGED");
            return new(Guid.NewGuid(), owner, request, stamps.AsReadOnly(), now, expires);
        }
    }
}
