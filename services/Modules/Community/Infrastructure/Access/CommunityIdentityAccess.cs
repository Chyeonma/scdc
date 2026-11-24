using Microsoft.Extensions.DependencyInjection;
using SCDC.BuildingBlocks.Infrastructure.Persistence;
using SCDC.Contracts.Identity;

namespace SCDC.Modules.Community.Infrastructure.Access;

internal sealed record CommunityAccountLease(IdentityPermit Permit)
{
    public DateTimeOffset SessionExpiresAt => Permit.ExpiresAt;
}

internal sealed class CommunityIdentityAccess(
    [FromKeyedServices(AccessOwner.Community)] IIdentityPermitIssuer issuer, TimeProvider clock)
{
    public async Task<IdentityPermit> AcquireAsync(AccountActor actor, IdentityPurpose purpose, Guid resource,
        Guid operation, CancellationToken ct, bool resuming = false)
    {
        var request=new PermitRequest(new(actor.UserId,actor.SessionId,actor.SecurityStamp,actor.AccessExpiresAt),purpose,resource,operation,
            Context:resuming?IdentityAccessContext.ResumingAcceptedOperation:IdentityAccessContext.HttpCaller);
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(ct);deadline.CancelAfter(TimeSpan.FromSeconds(2));
        IdentityPermit permit;
        try { permit=await issuer.AcquireAsync(request,deadline.Token).WaitAsync(deadline.Token); }
        catch(OperationCanceledException) when(!ct.IsCancellationRequested) { throw new AccessCheckException(); }
        catch(Npgsql.NpgsqlException) { throw new AccessCheckException(); }
        if(permit.Request!=request || permit.Owner!=AccessOwner.Community || permit.IssuedAt>clock.GetUtcNow().AddMilliseconds(100)
            || permit.ExpiresAt>permit.IssuedAt.AddSeconds(5) || permit.ExpiresAt>actor.AccessExpiresAt)
            throw new AccessCheckException("ACCESS_CHANGED");
        return permit;
    }

    public async Task<CommunityAccountLease> AdmitAsync(RelationalWorkScope scope, IdentityPermit permit, CancellationToken ct)
    {
        var actor = permit.Request.Actor;
        var expected = new[] { (Kind: (short)1, User: actor.UserId, Session: Guid.Empty),
            (Kind: (short)2, User: actor.UserId, Session: actor.SessionId) };
        if (permit.Owner != AccessOwner.Community || permit.Request.RoutingGeneration != 1
            || permit.Request.PeerId is not null || permit.Scopes.Count != 2
            || !permit.Scopes.Select(s => (s.Kind, s.UserId, s.SessionId)).ToHashSet().SetEquals(expected)
            || permit.Scopes.Any(s => s.Epoch <= 0) || permit.ExpiresAt - clock.GetUtcNow() < TimeSpan.FromMilliseconds(1500))
            throw new AccessCheckException("ACCESS_CHANGED");
        await using (var budget = scope.CreateCommand("SET LOCAL lock_timeout='500ms'; SET LOCAL statement_timeout='1s'; SET LOCAL transaction_timeout='1s'"))
            await budget.ExecuteNonQueryAsync(ct);
        foreach (var stamp in permit.Scopes.OrderBy(s => s.Kind))
        {
            await using (var create = scope.CreateCommand("""
                INSERT INTO community.access_fences(authority,scope_kind,user_id,session_id,min_epoch)
                VALUES(1,@kind,@user,@session,1) ON CONFLICT DO NOTHING
                """))
            {
                create.Parameters.AddWithValue("kind", stamp.Kind); create.Parameters.AddWithValue("user", stamp.UserId);
                create.Parameters.AddWithValue("session", stamp.SessionId); await create.ExecuteNonQueryAsync(ct);
            }
            await using var check = scope.CreateCommand("""
                SELECT min_epoch FROM community.access_fences WHERE authority=1 AND scope_kind=@kind
                AND user_id=@user AND session_id=@session AND server_id='00000000-0000-0000-0000-000000000000'
                AND channel_id='00000000-0000-0000-0000-000000000000' AND membership_id='00000000-0000-0000-0000-000000000000'
                AND permission_code='' FOR SHARE
                """);
            check.Parameters.AddWithValue("kind", stamp.Kind); check.Parameters.AddWithValue("user", stamp.UserId);
            check.Parameters.AddWithValue("session", stamp.SessionId);
            if (await check.ExecuteScalarAsync(ct) is not long floor || stamp.Epoch < floor)
                throw new AccessCheckException("ACCESS_CHANGED");
        }
        return new(permit);
    }

    public async Task EnsureAsync(RelationalWorkScope scope, CommunityAccountLease lease, CancellationToken ct)
    {
        if(lease.Permit.Request.Actor.AccessExpiresAt<=clock.GetUtcNow())throw new AccessCheckException("SESSION_INVALID");
        if (lease.Permit.ExpiresAt <= clock.GetUtcNow().AddMilliseconds(250))
            throw new AccessCheckException("ACCESS_CHANGED");
        await using var check = scope.CreateCommand("SELECT clock_timestamp()<@expires-interval '250 milliseconds'");
        check.Parameters.AddWithValue("expires", lease.Permit.ExpiresAt);
        if (await check.ExecuteScalarAsync(ct) is not true) throw new AccessCheckException("ACCESS_CHANGED");
    }
}
