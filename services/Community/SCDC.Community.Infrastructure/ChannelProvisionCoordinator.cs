using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.BuildingBlocks.Application.Text;
using SCDC.BuildingBlocks.Infrastructure.Outbox;
using SCDC.BuildingBlocks.Infrastructure.Persistence;
using SCDC.Contracts.Identity;
using SCDC.Contracts.Messaging;
using SCDC.Modules.Community.Features.Channels.Application;
using SCDC.Modules.Community.Features.Permissions.Application;
using SCDC.Modules.Community.Features.Permissions.Infrastructure;
using SCDC.Modules.Community.Infrastructure.Access;
using SCDC.Modules.Community.Infrastructure.Idempotency;
using SCDC.Modules.Community.Infrastructure.Persistence;

namespace SCDC.Modules.Community.Infrastructure;

internal sealed class ChannelProvisionCoordinator(RelationalWorkScopeFactory scopes, IServiceScopeFactory services,
    [FromKeyedServices(AccessOwner.Community)] IChannelProvisioning owner,
    IOptionsMonitor<CommunityOptions> options, ChannelReader reader, TransactionalOutbox outbox,
    TimeProvider clock, ILogger<ChannelProvisionCoordinator> logger)
{
    private sealed record Work(Guid Operation, Guid Channel, Guid Server, AccountActor Actor, Guid? ClientOperation,
        byte[] Fingerprint, string KeyId, short FingerprintVersion, string Name, string NameKey, string? Topic,
        DateTimeOffset CreatedAt, DateTimeOffset Deadline, short Phase, long DecisionVersion, bool DeliveryPending);
    private const string Columns = "operation_id,channel_id,server_id,actor_user_id,actor_session_id,actor_security_stamp,actor_access_expires_at,actor_membership_id,client_operation_id,fingerprint,key_id,fingerprint_version,name,name_key,topic,created_at,deadline,phase,decision_version,delivery_pending";

    public async Task<CreateChannelResult> CreateAsync(AccountActor actor, Guid server, CreateChannelCommand command, CancellationToken ct)
    {
        Work work; bool created;
        try { (work, created) = await ReserveAsync(actor, server, command, ct); }
        catch (NpgsqlException) { throw new ChannelOutcomeUnknownException(server, command.ClientOperationId); }
        var wait = Stopwatch.StartNew();
        try
        {
            while (work.Phase == 0 && wait.Elapsed < TimeSpan.FromSeconds(6))
            {
                await ProcessAsync(work.Operation, ct);
                work = await FindAsync(work.Operation, ct) ?? throw new ChannelOutcomeUnknownException(server, command.ClientOperationId);
                if (work.Phase == 0) await Task.Delay(25, ct);
            }
        }
        catch (NpgsqlException) { throw new ChannelOutcomeUnknownException(server, command.ClientOperationId); }
        if (work.Phase == 0) throw new ChannelOutcomeUnknownException(server, command.ClientOperationId);
        if (work.Phase == 2) throw Cancelled();
        // A replay reads current metadata under current authorization, never a cached response.
        await using var serviceScope = services.CreateAsyncScope();
        var management = serviceScope.ServiceProvider.GetRequiredService<CommunityManagementGuard>();
        var permit = await management.PrepareAsync(actor, server, false, ct);
        await using var local = await scopes.OpenAsync(ct);
        var lease = await management.AcquireAsync(local, actor, server, false, false, ct, permit, requiredPermission: null);
        var channel = await reader.StateAsync(local, server, work.Channel, false, ct)
            ?? throw PermissionFailure.Conflict("OPERATION_RESOURCE_REMOVED", "The created channel is no longer available.");
        if (!(await reader.EvaluateAsync(local, actor.UserId, server, channel, lease, ct)).CanView) throw PermissionFailure.Missing();
        await CommunityScopeStore.CheckAsync(local, [CommunityScopeStore.Channel(actor.UserId, server, work.Channel, lease.MembershipId), CommunityScopeStore.Lifecycle(server, work.Channel)], null, ct);
        await management.EnsureAsync(local, lease.Account, ct); await local.CommitAsync(ct);
        return new(channel.View, created);
    }

    private async Task<(Work Work, bool Created)> ReserveAsync(AccountActor actor, Guid server, CreateChannelCommand command, CancellationToken ct)
    {
        await using var serviceScope = services.CreateAsyncScope();
        var management = serviceScope.ServiceProvider.GetRequiredService<CommunityManagementGuard>();
        var permit = await management.PrepareAsync(actor, server, true, ct, IdentityPurpose.ChannelCreate);
        await using var local = await scopes.OpenAsync(ct);
        var lease = await management.AcquireAsync(local, actor, server, true, false, ct, permit, "manage_channels");
        Work? previous;
        await using (var query = local.CreateCommand($"SELECT {Columns} FROM community.channel_provisions WHERE actor_user_id=@actor AND server_id=@server AND client_operation_id=@client"))
        {
            query.Parameters.AddWithValue("actor", actor.UserId); query.Parameters.AddWithValue("server", server);
            query.Parameters.AddWithValue("client", command.ClientOperationId);
            await using var rows = await query.ExecuteReaderAsync(ct);
            previous = await rows.ReadAsync(ct) ? Read(rows) : null;
        }
        if (previous is not null)
        {
            var hash = OperationFingerprint.ComputeChannel(options.CurrentValue.Operations.GetKey(previous.KeyId), actor.UserId,
                server, command.ClientOperationId, command.Name!, command.Topic, 1, previous.FingerprintVersion);
            if (!CryptographicOperations.FixedTimeEquals(hash, previous.Fingerprint))
                throw PermissionFailure.Conflict("OPERATION_CONFLICT", "Operation key was used with different data.");
            await management.EnsureAsync(local, lease.Account, ct); await local.CommitAsync(ct);
            return (previous, false);
        }
        // A historical operation can outlive its channel metadata. Retain its
        // idempotency key even when there was no channel row to backfill.
        await using (var history = local.CreateCommand("SELECT fingerprint,key_id,fingerprint_version FROM community.operations WHERE actor_user_id=@actor AND kind='create_channel' AND scope_id=@server AND client_operation_id=@client"))
        {
            history.Parameters.AddWithValue("actor", actor.UserId); history.Parameters.AddWithValue("server", server);
            history.Parameters.AddWithValue("client", command.ClientOperationId);
            await using var rows = await history.ExecuteReaderAsync(ct);
            if (await rows.ReadAsync(ct))
            {
                var hash = OperationFingerprint.ComputeChannel(options.CurrentValue.Operations.GetKey(rows.GetString(1)),
                    actor.UserId, server, command.ClientOperationId, command.Name!, command.Topic, 1, rows.GetInt16(2));
                if (!CryptographicOperations.FixedTimeEquals(hash, rows.GetFieldValue<byte[]>(0)))
                    throw PermissionFailure.Conflict("OPERATION_CONFLICT", "Operation key was used with different data.");
                throw PermissionFailure.Conflict("OPERATION_RESOURCE_REMOVED", "The historical channel is no longer available.");
            }
        }
        if (lease.ServerVersion == int.MaxValue || lease.AccessVersion == int.MaxValue)
            throw PermissionFailure.Conflict("VERSION_LIMIT_REACHED", "The resource version limit has been reached.");
        var nameKey = UnicodeTextPolicy.NormalizeNameKey(command.Name!);
        await CheckNameAsync(local, server, nameKey, null, ct);
        var operation = Guid.CreateVersion7(); var channel = Guid.CreateVersion7();
        var keys = options.CurrentValue.Operations;
        var fingerprint = OperationFingerprint.ComputeChannel(keys.GetKey(keys.ActiveKeyId), actor.UserId, server,
            command.ClientOperationId, command.Name!, command.Topic, 1, 1);
        Work work;
        await using (var insert = local.CreateCommand($"""
            INSERT INTO community.channel_provisions(operation_id,channel_id,server_id,actor_user_id,actor_session_id,
                actor_security_stamp,actor_access_expires_at,actor_membership_id,client_operation_id,fingerprint,key_id,
                name,name_key,topic,created_at,deadline)
            VALUES(@op,@channel,@server,@actor,@session,@stamp,@expires,@member,@client,@fingerprint,@key,
                @name,@nameKey,@topic,clock_timestamp(),clock_timestamp()+interval '5 seconds') RETURNING {Columns}
            """))
        {
            insert.Parameters.AddWithValue("op", operation); insert.Parameters.AddWithValue("channel", channel);
            insert.Parameters.AddWithValue("server", server); insert.Parameters.AddWithValue("actor", actor.UserId);
            insert.Parameters.AddWithValue("session", actor.SessionId); insert.Parameters.AddWithValue("stamp", actor.SecurityStamp);
            insert.Parameters.AddWithValue("expires", actor.AccessExpiresAt); insert.Parameters.AddWithValue("member", lease.MembershipId);
            insert.Parameters.AddWithValue("client", command.ClientOperationId); insert.Parameters.AddWithValue("fingerprint", fingerprint);
            insert.Parameters.AddWithValue("key", keys.ActiveKeyId); insert.Parameters.AddWithValue("name", command.Name!);
            insert.Parameters.AddWithValue("nameKey", nameKey); insert.Parameters.AddWithValue("topic", NpgsqlTypes.NpgsqlDbType.Text, (object?)command.Topic ?? DBNull.Value);
            await using var rows = await insert.ExecuteReaderAsync(ct); await rows.ReadAsync(ct); work = Read(rows);
        }
        await management.EnsureAsync(local, lease.Account, ct); await local.CommitAsync(ct);
        return (work, true);
    }

    internal static async Task CheckNameAsync(RelationalWorkScope local, Guid server, string nameKey, Guid? exceptChannel, CancellationToken ct)
    {
        await using var query = local.CreateCommand("""
            SELECT EXISTS(SELECT 1 FROM community.channels WHERE server_id=@server AND name_key=@name
                AND status=1 AND deleted_at IS NULL AND(@except IS NULL OR space_id<>@except))
            OR EXISTS(SELECT 1 FROM community.channel_provisions WHERE server_id=@server AND name_key=@name AND phase=0)
            """);
        query.Parameters.AddWithValue("server", server); query.Parameters.AddWithValue("name", nameKey);
        query.Parameters.AddWithValue("except", NpgsqlTypes.NpgsqlDbType.Uuid, (object?)exceptChannel ?? DBNull.Value);
        if (await query.ExecuteScalarAsync(ct) is true)
            throw PermissionFailure.Conflict("NAME_CONFLICT", "A channel with this name already exists or is being prepared.");
    }

    public async Task<ChannelCreationStatus?> ReadStatusAsync(AccountActor actor, Guid server, Guid clientOperation, CancellationToken ct)
    {
        await using var serviceScope = services.CreateAsyncScope();
        var management = serviceScope.ServiceProvider.GetRequiredService<CommunityManagementGuard>();
        var permit = await management.PrepareAsync(actor, server, false, ct);
        await using var local = await scopes.OpenAsync(ct);
        var lease = await management.AcquireAsync(local, actor, server, false, false, ct, permit, requiredPermission: null);
        Work? work;
        await using (var query = local.CreateCommand($"SELECT {Columns} FROM community.channel_provisions WHERE actor_user_id=@actor AND server_id=@server AND client_operation_id=@client"))
        {
            query.Parameters.AddWithValue("actor", actor.UserId); query.Parameters.AddWithValue("server", server);
            query.Parameters.AddWithValue("client", clientOperation);
            await using var rows = await query.ExecuteReaderAsync(ct); work = await rows.ReadAsync(ct) ? Read(rows) : null;
        }
        if (work is null) return null;
        if (work.Phase == 1)
        {
            var channel = await reader.StateAsync(local, server, work.Channel, false, ct);
            if (channel is null || !(await reader.EvaluateAsync(local, actor.UserId, server, channel, lease, ct)).CanView)
                throw PermissionFailure.Missing();
            await CommunityScopeStore.CheckAsync(local, [CommunityScopeStore.Channel(actor.UserId, server, work.Channel, lease.MembershipId), CommunityScopeStore.Lifecycle(server, work.Channel)], null, ct);
        }
        else
        {
            if (!lease.Permissions.Contains("manage_channels")) throw PermissionFailure.Missing();
            await CommunityScopeStore.CheckAsync(local, [CommunityScopeStore.Management(actor.UserId, server, lease.MembershipId, "manage_channels")], null, ct);
        }
        await management.EnsureAsync(local, lease.Account, ct); await local.CommitAsync(ct);
        return new(work.Operation, clientOperation, work.Phase switch { 0 => "preparing", 1 => "completed", _ => "cancelled" },
            (work.DecisionVersion + 1).ToString(CultureInfo.InvariantCulture), work.Phase != 0 && work.DeliveryPending, work.Phase == 1 ? work.Channel : null);
    }

    public async Task ProcessAsync(Guid operation, CancellationToken ct)
    {
        var leaseToken = Guid.NewGuid();
        Work? work;
        await using (var claim = await scopes.OpenAsync(ct))
        {
            await using var query = claim.CreateCommand($"""
                UPDATE community.channel_provisions SET lease_token=@lease,lease_until=clock_timestamp()+interval '6 seconds',version=version+1
                WHERE operation_id=@op AND delivery_pending AND(lease_until IS NULL OR lease_until<clock_timestamp()) RETURNING {Columns}
                """);
            query.Parameters.AddWithValue("op", operation); query.Parameters.AddWithValue("lease", leaseToken);
            await using (var rows = await query.ExecuteReaderAsync(ct)) work = await rows.ReadAsync(ct) ? Read(rows) : null;
            await claim.CommitAsync(ct);
        }
        if (work is null) return;
        try
        {
            if (work.Phase == 0)
            {
                if (work.Deadline <= clock.GetUtcNow() || work.Actor.AccessExpiresAt <= clock.GetUtcNow())
                    await CancelAsync(work, ct);
                else
                {
                    try
                    {
                        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        deadline.CancelAfter(TimeSpan.FromSeconds(2));
                        var prepared = await owner.PrepareAsync(new(work.Operation, work.Channel, work.Server, work.Actor.UserId,
                            work.CreatedAt, work.Fingerprint), deadline.Token).WaitAsync(deadline.Token);
                        if (prepared.OperationId != work.Operation || prepared.ChannelId != work.Channel || prepared.Phase != ProvisionPhase.Prepared || prepared.DecisionVersion != 0)
                            throw new ProvisionConflictException("Unexpected prepare response.");
                        await CompleteAsync(work, ct);
                    }
                    catch (AccessCheckException error) when (error.Code is "SESSION_INVALID" or "Common.Unauthorized" or "ACCOUNT_ACCESS_DENIED" or "ACCESS_CHANGED")
                    { await CancelAsync(work, ct); }
                    catch (PermissionFailure) { await CancelAsync(work, ct); }
                    catch (ProvisionConflictException) { await CancelAsync(work, ct); }
                }
                work = await FindAsync(operation, ct) ?? throw new ProvisionConflictException("Accepted operation missing.");
                if (work.Phase == 0 && work.Deadline <= clock.GetUtcNow())
                { await CancelAsync(work, ct); work = (await FindAsync(operation, ct))!; }
            }
            if (work.Phase != 0)
            {
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct); deadline.CancelAfter(TimeSpan.FromSeconds(2));
                var decision = new ChannelDecision(work.Operation, work.Channel, work.Fingerprint, work.DecisionVersion);
                var status = await (work.Phase == 1 ? owner.ConfirmAsync(decision, deadline.Token) : owner.CancelAsync(decision, deadline.Token)).WaitAsync(deadline.Token);
                if (status.OperationId != work.Operation || status.ChannelId != work.Channel || status.DecisionVersion != work.DecisionVersion
                    || status.Phase != (work.Phase == 1 ? ProvisionPhase.Ready : ProvisionPhase.Cancelled) || status.CleanupPending)
                    throw new ProvisionConflictException("Owner did not acknowledge the accepted decision.");
                await using var local = await scopes.OpenAsync(ct);
                await using var delivered = local.CreateCommand("UPDATE community.channel_provisions SET delivery_pending=false,version=version+1 WHERE operation_id=@op AND phase=@phase AND decision_version=@version");
                delivered.Parameters.AddWithValue("op", operation); delivered.Parameters.AddWithValue("phase", work.Phase);
                delivered.Parameters.AddWithValue("version", work.DecisionVersion); await delivered.ExecuteNonQueryAsync(ct); await local.CommitAsync(ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception error)
        {
            // The owner may already have committed. Keep the same operation and decision.
            logger.LogWarning("Channel provisioning {OperationId} deferred ({FailureType})", operation, error.GetType().Name);
        }
        finally
        {
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            try
            {
                await using var local = await scopes.OpenAsync(cleanup.Token);
                await using var release = local.CreateCommand("""
                    UPDATE community.channel_provisions SET lease_token=NULL,lease_until=NULL,
                        next_attempt_at=CASE WHEN phase=0 THEN LEAST(deadline,clock_timestamp()+interval '100 milliseconds')
                            ELSE clock_timestamp()+interval '1 second'*LEAST(30,power(2,least(version/2,5))) END,
                        version=version+1 WHERE operation_id=@op AND lease_token=@lease
                    """);
                release.Parameters.AddWithValue("op", operation); release.Parameters.AddWithValue("lease", leaseToken);
                await release.ExecuteNonQueryAsync(cleanup.Token); await local.CommitAsync(cleanup.Token);
            }
            catch (Exception error) { logger.LogWarning("Channel work lease will expire ({FailureType})", error.GetType().Name); }
        }
    }

    private async Task CompleteAsync(Work work, CancellationToken ct)
    {
        await using var serviceScope = services.CreateAsyncScope();
        var access = serviceScope.ServiceProvider.GetRequiredService<CommunityIdentityAccess>();
        var management = serviceScope.ServiceProvider.GetRequiredService<CommunityManagementGuard>();
        var permit = await access.AcquireAsync(work.Actor, IdentityPurpose.ChannelCreate, work.Server, work.Operation, ct, resuming: true);
        await using var local = await scopes.OpenAsync(ct);
        var lease = await management.AcquireAsync(local, work.Actor, work.Server, true, false, ct, permit, "manage_channels");
        await using (var decision = local.CreateCommand("SELECT phase,deadline>clock_timestamp() FROM community.channel_provisions WHERE operation_id=@op FOR UPDATE"))
        {
            decision.Parameters.AddWithValue("op", work.Operation);
            await using var rows = await decision.ExecuteReaderAsync(ct); await rows.ReadAsync(ct);
            if (rows.GetInt16(0) != 0) return;
            if (!rows.GetBoolean(1)) throw Cancelled();
        }
        if (lease.ServerVersion == int.MaxValue || lease.AccessVersion == int.MaxValue)
            throw PermissionFailure.Conflict("VERSION_LIMIT_REACHED", "The resource version limit has been reached.");
        await CommunityScopeStore.CheckAsync(local, [CommunityScopeStore.Lifecycle(work.Server, work.Channel)], null, ct);
        await using (var lifecycle = local.CreateCommand("SELECT current_epoch=1 FROM community.access_scopes WHERE authority=2 AND scope_kind=6 AND server_id=@server AND channel_id=@channel AND user_id='00000000-0000-0000-0000-000000000000' AND session_id='00000000-0000-0000-0000-000000000000' AND membership_id='00000000-0000-0000-0000-000000000000' AND permission_code=''"))
        {
            lifecycle.Parameters.AddWithValue("server", work.Server); lifecycle.Parameters.AddWithValue("channel", work.Channel);
            if (await lifecycle.ExecuteScalarAsync(ct) is not true) throw Cancelled();
        }
        await using (var insert = local.CreateCommand("""
            INSERT INTO community.channels(space_id,server_id,name,name_key,topic,kind,default_view,created_at,updated_at)
            VALUES(@channel,@server,@name,@nameKey,@topic,1,1,@created,@created);
            INSERT INTO community.operations(actor_user_id,kind,scope_id,client_operation_id,fingerprint_version,key_id,fingerprint,resource_id)
            VALUES(@actor,'create_channel',@server,@client,@fv,@key,@fingerprint,@channel);
            UPDATE community.servers SET access_version=access_version+1 WHERE id=@server
            """))
        {
            insert.Parameters.AddWithValue("channel", work.Channel); insert.Parameters.AddWithValue("server", work.Server);
            insert.Parameters.AddWithValue("name", work.Name); insert.Parameters.AddWithValue("nameKey", work.NameKey);
            insert.Parameters.AddWithValue("topic", NpgsqlTypes.NpgsqlDbType.Text, (object?)work.Topic ?? DBNull.Value);
            insert.Parameters.AddWithValue("created", work.CreatedAt); insert.Parameters.AddWithValue("actor", work.Actor.UserId);
            insert.Parameters.AddWithValue("client", work.ClientOperation!.Value); insert.Parameters.AddWithValue("fv", work.FingerprintVersion);
            insert.Parameters.AddWithValue("key", work.KeyId); insert.Parameters.AddWithValue("fingerprint", work.Fingerprint);
            await insert.ExecuteNonQueryAsync(ct);
        }
        await outbox.AppendAsync(local, "Community.AccessChanged.v1", "community.server", work.Server, lease.ServerVersion + 1,
            new { serverId = work.Server, channelId = work.Channel, accessVersion = (lease.AccessVersion + 1).ToString(CultureInfo.InvariantCulture), channelAccessVersion = "1", cause = "channel_created" }, ct);
        await management.EnsureAsync(local, lease.Account, ct);
        await using (var complete = local.CreateCommand("""
            UPDATE community.channel_provisions SET phase=1,decision_version=1,completed_at=clock_timestamp(),version=version+1
            WHERE operation_id=@op AND phase=0 AND deadline>clock_timestamp() RETURNING operation_id
            """))
        {
            complete.Parameters.AddWithValue("op", work.Operation);
            if (await complete.ExecuteScalarAsync(ct) is null) throw new PermissionFailure(Error.Conflict("CHANNEL_CREATION_CANCELLED", "The channel creation deadline elapsed."));
        }
        await local.CommitAsync(ct);
    }

    private async Task CancelAsync(Work work, CancellationToken ct)
    {
        await using var local = await scopes.OpenAsync(ct);
        await using (var budget = local.CreateCommand("SET LOCAL lock_timeout='500ms'; SET LOCAL statement_timeout='1s'; SET LOCAL transaction_timeout='1s'")) await budget.ExecuteNonQueryAsync(ct);
        // Same lock order as completion. Cancellation is system work, even after the actor loses access.
        await using (var server = local.CreateCommand("SELECT id FROM community.servers WHERE id=@server FOR UPDATE"))
        { server.Parameters.AddWithValue("server", work.Server); await server.ExecuteScalarAsync(ct); }
        await using var cancel = local.CreateCommand("""
            UPDATE community.channel_provisions SET phase=2,decision_version=1,completed_at=clock_timestamp(),version=version+1
            WHERE operation_id=@op AND phase=0
            """);
        cancel.Parameters.AddWithValue("op", work.Operation); await cancel.ExecuteNonQueryAsync(ct); await local.CommitAsync(ct);
    }

    private async Task<Work?> FindAsync(Guid operation, CancellationToken ct)
    {
        await using var local = await scopes.OpenAsync(ct);
        await using var query = local.CreateCommand($"SELECT {Columns} FROM community.channel_provisions WHERE operation_id=@op");
        query.Parameters.AddWithValue("op", operation);
        await using var rows = await query.ExecuteReaderAsync(ct); return await rows.ReadAsync(ct) ? Read(rows) : null;
    }
    private static Work Read(NpgsqlDataReader rows) => new(rows.GetGuid(0), rows.GetGuid(1), rows.GetGuid(2),
        new(rows.GetGuid(3), rows.IsDBNull(4) ? Guid.Empty : rows.GetGuid(4), rows.IsDBNull(5) ? Guid.Empty : rows.GetGuid(5),
            rows.IsDBNull(6) ? DateTimeOffset.MinValue : rows.GetFieldValue<DateTimeOffset>(6), rows.IsDBNull(7) ? null : rows.GetGuid(7)),
        rows.IsDBNull(8) ? null : rows.GetGuid(8), rows.GetFieldValue<byte[]>(9), rows.GetString(10), rows.GetInt16(11),
        rows.GetString(12), rows.GetString(13), rows.IsDBNull(14) ? null : rows.GetString(14), rows.GetFieldValue<DateTimeOffset>(15),
        rows.GetFieldValue<DateTimeOffset>(16), rows.GetInt16(17), rows.GetInt64(18), rows.GetBoolean(19));
    private static PermissionFailure Cancelled() => PermissionFailure.Conflict("CHANNEL_CREATION_CANCELLED", "Channel creation was cancelled. Start a new operation to retry.");
}
