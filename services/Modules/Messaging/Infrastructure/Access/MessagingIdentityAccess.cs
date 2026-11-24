using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.BuildingBlocks.Infrastructure;
using SCDC.Contracts.Identity;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Access;

internal sealed class MessagingAccessFailure(Error error) : Exception("Messaging access could not be confirmed.")
{
    public Error Error { get; } = error;
}
internal sealed class MessagingIdentityAccess([FromKeyedServices(AccessOwner.Messaging)] IIdentityPermitIssuer issuer, TimeProvider clock)
{
    public async Task<IdentityPermit> AcquireAsync(AuthenticatedSession actor, IdentityPurpose purpose, Guid resource,
        CancellationToken ct, Guid? peer = null, Guid? operation = null)
    {
        var request = new PermitRequest(actor, purpose, resource, operation ?? Guid.NewGuid(), peer);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct); deadline.CancelAfter(TimeSpan.FromSeconds(2));
        IdentityPermit permit;
        try { permit = await issuer.AcquireAsync(request, deadline.Token).WaitAsync(deadline.Token); }
        catch (AccessCheckException failure) { throw Failure(failure.Code); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw Failure("AUTHORITY_UNAVAILABLE"); }
        catch (Exception failure) when (DatabaseAvailability.IsUnavailable(failure)) { throw Failure("AUTHORITY_UNAVAILABLE"); }
        if (permit.Request != request || permit.Owner != AccessOwner.Messaging || permit.Request.Context != IdentityAccessContext.HttpCaller
            || permit.IssuedAt > clock.GetUtcNow().AddMilliseconds(100) || permit.ExpiresAt > permit.IssuedAt.AddSeconds(5)
            || permit.ExpiresAt > actor.AccessExpiresAt) throw Failure("ACCESS_CHANGED");
        return permit;
    }
    public async Task AdmitAsync(MessagingDbContext db, IdentityPermit permit, CancellationToken ct)
    {
        var actor = permit.Request.Actor;
        var expected = new HashSet<(short Kind, Guid User, Guid Session)> { (1, actor.UserId, Guid.Empty), (2, actor.UserId, actor.SessionId) };
        var needsPeer = permit.Request.Purpose is IdentityPurpose.DmOpenNew or IdentityPurpose.DmSendNew;
        if (needsPeer && permit.Request.PeerId is Guid peer) expected.Add((1, peer, Guid.Empty));
        if (permit.Owner != AccessOwner.Messaging || permit.Request.RoutingGeneration != 1
            || permit.Request.Purpose is < IdentityPurpose.DmOpenNew or > IdentityPurpose.DmDeleteOwn
            || needsPeer != (permit.Request.PeerId is not null) || permit.Scopes.Count != expected.Count
            || !permit.Scopes.Select(s => (s.Kind, s.UserId, s.SessionId)).ToHashSet().SetEquals(expected)
            || permit.Scopes.Any(s => s.Epoch <= 0) || permit.ExpiresAt - clock.GetUtcNow() < TimeSpan.FromMilliseconds(1500)) throw Failure("ACCESS_CHANGED");
        await db.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout='500ms'; SET LOCAL statement_timeout='1s'; SET LOCAL transaction_timeout='1s'", ct);
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        var transaction = (NpgsqlTransaction)(db.Database.CurrentTransaction?.GetDbTransaction() ?? throw new InvalidOperationException("A local Messaging transaction is required."));
        foreach (var stamp in permit.Scopes.OrderBy(s => s.Kind).ThenBy(s => s.UserId.ToString("D"), StringComparer.Ordinal).ThenBy(s => s.SessionId.ToString("D"), StringComparer.Ordinal))
        {
            await using (var create = new NpgsqlCommand("INSERT INTO messaging.access_fences(authority,scope_kind,user_id,session_id,min_epoch) VALUES(1,@kind,@user,@session,1) ON CONFLICT DO NOTHING", connection, transaction))
            { Bind(create, stamp); await create.ExecuteNonQueryAsync(ct); }
            await using var read = new NpgsqlCommand("""
                SELECT min_epoch FROM messaging.access_fences WHERE authority=1 AND scope_kind=@kind AND user_id=@user AND session_id=@session
                AND server_id='00000000-0000-0000-0000-000000000000' AND channel_id='00000000-0000-0000-0000-000000000000'
                AND membership_id='00000000-0000-0000-0000-000000000000' AND permission_code='' FOR SHARE
                """, connection, transaction);
            Bind(read, stamp);
            if (await read.ExecuteScalarAsync(ct) is not long floor || stamp.Epoch < floor) throw Failure("ACCESS_CHANGED");
        }
    }
    public async Task EnsureAsync(MessagingDbContext db, IdentityPermit permit, CancellationToken ct)
    {
        if (permit.Request.Actor.AccessExpiresAt <= clock.GetUtcNow()) throw Failure("Common.Unauthorized");
        if (permit.ExpiresAt <= clock.GetUtcNow().AddMilliseconds(250)) throw Failure("ACCESS_CHANGED");
        if (!await db.Database.SqlQuery<bool>($"SELECT clock_timestamp() < {permit.ExpiresAt}-interval '250 milliseconds' AS \"Value\"").SingleAsync(ct)) throw Failure("ACCESS_CHANGED");
    }
    private static void Bind(NpgsqlCommand command, IdentityScope scope)
    { command.Parameters.AddWithValue("kind", scope.Kind); command.Parameters.AddWithValue("user", scope.UserId); command.Parameters.AddWithValue("session", scope.SessionId); }
    private static MessagingAccessFailure Failure(string code) => new(code switch
    {
        "Common.Unauthorized" or "ACCOUNT_ACCESS_DENIED" => Error.Unauthorized("Common.Unauthorized", "The current session is not available."),
        "RESOURCE_NOT_FOUND" => Error.NotFound(code, "The requested conversation is not available."),
        "ACCESS_CHANGED" => Error.Conflict(code, "Access changed. Reload before trying again."),
        "ACCESS_PENDING" => Error.ServiceUnavailable(code, "Access is being changed. Reconcile before trying again."),
        _ => Error.ServiceUnavailable("AUTHORITY_UNAVAILABLE", "The service cannot confirm this request.")
    });
}
