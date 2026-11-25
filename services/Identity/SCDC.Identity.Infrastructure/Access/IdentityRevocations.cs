using System.Diagnostics;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SCDC.Contracts.Identity;
using SCDC.Modules.Identity.Application;
using SCDC.Modules.Identity.Domain;
using SCDC.Modules.Identity.Infrastructure.Persistence;

namespace SCDC.Modules.Identity.Infrastructure.Services;

internal sealed record IdentitySecurityChange(string Reason, string EventType, Guid[] SessionIds,
    RequestContext Context, string? PasswordHash = null, Guid? ResetTokenId = null, Guid? RequestSessionId = null,
    int? AccountStatus = null);
internal sealed record AcceptedIdentityChange(IdentitySecurityChange Change, IdentityScope[] Scopes);

internal sealed class IdentityRevocations(IConfiguration configuration, IServiceScopeFactory scopes,
    IEnumerable<IIdentityFenceOwner> owners, SessionPublisher publisher) : IRevocationStatusReader
{
    private readonly string database = ModuleDatabaseConnection.Get(configuration, "Identity");
    private readonly IIdentityFenceOwner[] fenceOwners = owners.OrderBy(o => o.Owner).ToArray();

    // Caller has locked the user and validated the command. Persist only accepted, typed work;
    // no plaintext passwords, bearer/refresh tokens or email-link tokens enter the journal.
    public async Task AcceptAndCompleteAsync(IdentityDbContext db, Guid userId, IdentitySecurityChange change,
        IDbContextTransaction transaction, CancellationToken ct)
    {
        if (fenceOwners.Length != 2 || !fenceOwners.Select(o => o.Owner).SequenceEqual(new[] { AccessOwner.Community, AccessOwner.Messaging }))
            throw new AccessCheckException();
        var operation = Guid.CreateVersion7();
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        var tx = (NpgsqlTransaction)transaction.GetDbTransaction();
        var stamps = new List<IdentityScope>();
        var targets = change.SessionIds.Select(id => ((short)2, id)).ToList();
        if (change.AccountStatus is not null) targets.Insert(0, ((short)1, Guid.Empty));
        foreach (var (kind, session) in targets.OrderBy(t => t.Item1).ThenBy(t => t.Item2.ToString("D"), StringComparer.Ordinal))
        {
            await using var prepare = new NpgsqlCommand("""
                INSERT INTO identity.access_scopes(scope_kind,user_id,session_id) VALUES(@kind,@user,@session)
                ON CONFLICT DO NOTHING;
                UPDATE identity.access_scopes SET pending_operation_id=@operation,version=version+1
                WHERE scope_kind=@kind AND user_id=@user AND session_id=@session
                AND pending_operation_id IS NULL RETURNING current_epoch+1
                """, connection, tx);
            prepare.Parameters.AddWithValue("kind", kind); prepare.Parameters.AddWithValue("user", userId);
            prepare.Parameters.AddWithValue("session", session); prepare.Parameters.AddWithValue("operation", operation);
            var epoch = await prepare.ExecuteScalarAsync(ct);
            if (epoch is not long next) throw new AccessCheckException("ACCESS_PENDING");
            stamps.Add(new(kind, userId, session, next));
        }
        var payload = JsonSerializer.Serialize(new AcceptedIdentityChange(change, stamps.ToArray()));
        var fingerprint = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        await using (var accept = new NpgsqlCommand("""
            INSERT INTO identity.access_changes(operation_id,user_id,routing_generation,owners,payload,command_fingerprint)
            VALUES(@operation,@user,1,ARRAY['Community','Messaging'],@payload::jsonb,@fingerprint)
            """, connection, tx))
        {
            accept.Parameters.AddWithValue("operation", operation); accept.Parameters.AddWithValue("user", userId);
            accept.Parameters.AddWithValue("payload", payload);
            accept.Parameters.AddWithValue("fingerprint", fingerprint);
            await accept.ExecuteNonQueryAsync(ct);
        }
        await transaction.CommitAsync(ct);
        // Dispose the committed transaction before using a fresh scope/connection to finalize.
        await transaction.DisposeAsync();
        var waiting = Stopwatch.StartNew();
        while (waiting.Elapsed < TimeSpan.FromSeconds(5) && !ct.IsCancellationRequested)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
            deadline.CancelAfter(TimeSpan.FromSeconds(5) - waiting.Elapsed);
            try { await ProcessAsync(operation, deadline.Token); }
            catch (Exception error) when (error is NpgsqlException or AccessCheckException or TimeoutException) { }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested) { break; }
            var status = await ReadAsync(operation, userId, ct);
            if (status?.Phase == "committed") return;
            await Task.Delay(50, ct);
        }
        throw new AccessCheckException("ACCESS_PENDING", operation);
    }

    public async Task<bool> ProcessAsync(Guid operation, CancellationToken ct)
    {
        var lease = Guid.NewGuid();
        await using var connection = new NpgsqlConnection(database);
        await connection.OpenAsync(ct);
        Guid user;
        AcceptedIdentityChange accepted;
        await using (var claim = new NpgsqlCommand("""
            UPDATE identity.access_changes SET lease_token=@lease,lease_until=clock_timestamp()+interval '10 seconds',version=version+1
            WHERE operation_id=@operation AND phase=0 AND next_attempt_at<=clock_timestamp()
            AND (lease_until IS NULL OR lease_until<clock_timestamp())
            RETURNING user_id,payload::text
            """, connection))
        {
            claim.Parameters.AddWithValue("operation", operation); claim.Parameters.AddWithValue("lease", lease);
            await using var row = await claim.ExecuteReaderAsync(ct);
            if (!await row.ReadAsync(ct)) return false;
            user = row.GetGuid(0);
            accepted = JsonSerializer.Deserialize<AcceptedIdentityChange>(row.GetString(1))!;
        }
        try
        {
            if (accepted.Scopes.Length > 0)
                foreach (var owner in fenceOwners)
                {
                    var batch = new IdentityFenceBatch(operation, operation, 1, Array.AsReadOnly(accepted.Scopes));
                    using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    deadline.CancelAfter(TimeSpan.FromSeconds(2));
                    IdentityFenceReceipt receipt;
                    try { receipt = await owner.ApplyAsync(batch, deadline.Token).WaitAsync(deadline.Token); }
                    catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new AccessCheckException(); }
                    if (receipt.Owner != owner.Owner || receipt.Batch.OperationId != batch.OperationId
                        || receipt.Batch.BatchId != batch.BatchId || receipt.Batch.RoutingGeneration != batch.RoutingGeneration
                        || !receipt.Batch.Scopes.SequenceEqual(batch.Scopes)) throw new AccessCheckException();
                    await using var ack = new NpgsqlCommand("""
                    INSERT INTO identity.access_change_acks(operation_id,batch_id,owner,routing_generation,installed_scopes)
                    VALUES(@operation,@operation,@owner,1,@scopes::jsonb) ON CONFLICT DO NOTHING
                    """, connection);
                    ack.Parameters.AddWithValue("operation", operation); ack.Parameters.AddWithValue("owner", owner.Owner.ToString());
                    ack.Parameters.AddWithValue("scopes", JsonSerializer.Serialize(accepted.Scopes)); await ack.ExecuteNonQueryAsync(ct);
                }
            // Pending scope publication denies admission before final DB commit.
            await publisher.PublishUserAsync(user, ct);
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await db.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout='500ms'; SET LOCAL statement_timeout='2s'", ct);
            await db.LockUserAsync(user, ct, allowPending: true);
            var current = (NpgsqlConnection)db.Database.GetDbConnection();
            var local = (NpgsqlTransaction)tx.GetDbTransaction();
            await using (var check = new NpgsqlCommand("""
                SELECT phase=0 AND lease_token=@lease AND lease_until>clock_timestamp()
                AND (@empty OR (SELECT count(*) FROM identity.access_change_acks a WHERE a.operation_id=c.operation_id)=2)
                FROM identity.access_changes c WHERE operation_id=@operation FOR UPDATE
                """, current, local))
            {
                check.Parameters.AddWithValue("lease", lease); check.Parameters.AddWithValue("operation", operation);
                check.Parameters.AddWithValue("empty", accepted.Scopes.Length == 0);
                if (await check.ExecuteScalarAsync(ct) is not true) return false;
            }
            var now = DateTimeOffset.UtcNow;
            var plan = accepted.Change;
            var sessions = await db.AuthSessions.Include(s => s.RefreshTokens)
                .Where(s => s.UserId == user && plan.SessionIds.Contains(s.Id)).ToListAsync(ct);
            foreach (var session in sessions) IdentityData.RevokeSession(session, now, plan.Reason);
            var account = await db.Users.Include(u => u.SecurityState).Include(u => u.PasswordCredential).SingleAsync(u => u.Id == user, ct);
            if (plan.PasswordHash is not null)
            {
                var password = account.PasswordCredential ?? throw new AccessCheckException();
                password.PasswordHash = plan.PasswordHash; password.HashAlgorithm = "aspnetcore-identity-v3";
                password.PasswordVersion++; password.PasswordChangedAt = now; password.RequiresChange = false; password.UpdatedAt = now;
                var security = account.SecurityState ?? throw new AccessCheckException();
                security.SecurityStamp = Guid.NewGuid(); security.FailedLoginCount = 0; security.LockedUntil = null; security.UpdatedAt = now;
                account.UpdatedAt = now;
                var resetTokens = await db.AccountTokens.Where(t => t.UserId == user && t.Purpose == AccountTokenPurpose.ResetPassword && t.ConsumedAt == null).ToListAsync(ct);
                foreach (var token in resetTokens) token.ConsumedAt = now;
                db.OutboxEvents.Add(IdentityData.Outbox("Identity.PasswordChanged", user, account.Version, now, new { user_id = user, reason = plan.Reason }));
            }
            if (plan.AccountStatus is int state) { account.Status = (UserStatus)state; account.UpdatedAt = now; }
            db.SecurityEvents.Add(IdentityData.SecurityEvent(user, plan.EventType, plan.Context, now, new { operation_id = operation, session_id = plan.RequestSessionId }));
            await db.SaveChangesAsync(ct);
            await using (var finish = new NpgsqlCommand("""
                UPDATE identity.access_scopes SET current_epoch=current_epoch+1,pending_operation_id=NULL,
                    retired=(scope_kind=2),version=version+1 WHERE pending_operation_id=@operation;
                UPDATE identity.access_changes SET phase=1,version=version+1,committed_at=clock_timestamp(),
                    lease_until=NULL,lease_token=NULL WHERE operation_id=@operation AND lease_token=@lease
                """, current, local))
            { finish.Parameters.AddWithValue("operation", operation); finish.Parameters.AddWithValue("lease", lease); await finish.ExecuteNonQueryAsync(ct); }
            await tx.CommitAsync(ct);
            // The durable deny is already in Redis. Publication now supplies the final reason/stamp.
            await publisher.PublishUserAsync(user, ct);
            return true;
        }
        catch
        {
            using var retryBudget = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await using var retry = new NpgsqlCommand("""
                UPDATE identity.access_changes SET lease_token=NULL,lease_until=NULL,attempt_count=attempt_count+1,version=version+1,
                next_attempt_at=clock_timestamp()+make_interval(secs=>LEAST(30,power(2,LEAST(attempt_count,5))) * (0.8+random()*0.4))
                WHERE operation_id=@operation AND phase=0 AND lease_token=@lease
                """, connection);
            retry.Parameters.AddWithValue("operation", operation); retry.Parameters.AddWithValue("lease", lease);
            await retry.ExecuteNonQueryAsync(retryBudget.Token);
            throw;
        }
    }

    public async Task<RevocationStatus?> ReadAsync(Guid operationId, Guid userId, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(database); await connection.OpenAsync(ct);
        await using var query = new NpgsqlCommand("SELECT phase,version FROM identity.access_changes WHERE operation_id=@operation AND user_id=@user", connection);
        query.Parameters.AddWithValue("operation", operationId); query.Parameters.AddWithValue("user", userId);
        await using var row = await query.ExecuteReaderAsync(ct);
        return await row.ReadAsync(ct) ? new(operationId, row.GetInt16(0) switch { 0 => "fencing", 1 => "committed", _ => "aborted" }, row.GetInt64(1), false) : null;
    }
}
