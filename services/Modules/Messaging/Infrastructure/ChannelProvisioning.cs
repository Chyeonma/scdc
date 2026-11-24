using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Npgsql;
using SCDC.BuildingBlocks.Infrastructure;
using SCDC.Contracts.Messaging;
using SCDC.Contracts.Identity;
using SCDC.Contracts.Community;

namespace SCDC.Modules.Messaging.Infrastructure;

internal sealed class ChannelProvisioning(IConfiguration configuration, TimeProvider clock) : IChannelProvisioning
{
    private async Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
    {
        var connection = new NpgsqlConnection(ModuleDatabaseConnection.Get(configuration, "Messaging"));
        try { await connection.OpenAsync(ct); return connection; }
        catch { await connection.DisposeAsync(); throw; }
    }

    public async Task<ProvisionStatus> PrepareAsync(PrepareChannel request, CancellationToken ct)
    {
        Validate(request.OperationId, request.ChannelId, request.Fingerprint, request.RoutingGeneration, request.ChannelEpoch);
        if (request.ServerId == Guid.Empty || request.ActorId == Guid.Empty)
            throw new ProvisionConflictException("Invalid channel producer data.");
        await using var connection = await OpenAsync(ct);
        await using var tx = await connection.BeginTransactionAsync(ct);
        await GateAsync(connection, tx, request.OperationId, ct);
        var previous = await ReadAsync(connection, tx, request.OperationId, request.ChannelId, request.Fingerprint, ct);
        if (previous is not null)
        {
            if (previous.Phase != ProvisionPhase.Cancelled)
            {
                await using var identity = new NpgsqlCommand("SELECT server_id=@server AND actor_user_id=@actor AND created_at=@created FROM messaging.channel_provisions WHERE operation_id=@op", connection, tx);
                identity.Parameters.AddWithValue("server", request.ServerId); identity.Parameters.AddWithValue("actor", request.ActorId);
                identity.Parameters.AddWithValue("created", request.CreatedAt); identity.Parameters.AddWithValue("op", request.OperationId);
                if (await identity.ExecuteScalarAsync(ct) is not true) throw new ProvisionConflictException("Provision identity differs.");
                await CheckLifecycleAsync(connection, tx, request.ServerId, request.ChannelId, request.ChannelEpoch, ct);
                await CheckSpaceAsync(connection, tx, request.OperationId, request.ChannelId, ct);
            }
            await tx.CommitAsync(ct);
            return previous;
        }
        await CheckLifecycleAsync(connection, tx, request.ServerId, request.ChannelId, request.ChannelEpoch, ct);
        await using (var insert = new NpgsqlCommand("""
            INSERT INTO messaging.channel_provisions(operation_id,channel_id,server_id,actor_user_id,created_at,
                fingerprint,channel_epoch,routing_generation,phase,decision_version)
            VALUES(@op,@channel,@server,@actor,@created,@fingerprint,1,1,0,0);
            INSERT INTO messaging.spaces(id,space_type,created_by_user_id,created_at,updated_at,provision_operation_id,provision_state)
            VALUES(@channel,3,@actor,@created,@created,@op,0)
            """, connection, tx))
        {
            Bind(insert, request.OperationId, request.ChannelId, request.Fingerprint);
            insert.Parameters.AddWithValue("server", request.ServerId); insert.Parameters.AddWithValue("actor", request.ActorId);
            insert.Parameters.AddWithValue("created", request.CreatedAt); await insert.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
        return new(request.OperationId, request.ChannelId, ProvisionPhase.Prepared, 0);
    }

    public Task<ProvisionStatus> ConfirmAsync(ChannelDecision request, CancellationToken ct) => DecideAsync(request, false, ct);
    public Task<ProvisionStatus> CancelAsync(ChannelDecision request, CancellationToken ct) => DecideAsync(request, true, ct);

    private async Task<ProvisionStatus> DecideAsync(ChannelDecision request, bool cancel, CancellationToken ct)
    {
        Validate(request.OperationId, request.ChannelId, request.Fingerprint, request.RoutingGeneration, request.ChannelEpoch);
        if (request.DecisionVersion != 1) throw new ProvisionConflictException("Invalid decision version.");
        await using var connection = await OpenAsync(ct);
        await using var tx = await connection.BeginTransactionAsync(ct);
        await GateAsync(connection, tx, request.OperationId, ct);
        var previous = await ReadAsync(connection, tx, request.OperationId, request.ChannelId, request.Fingerprint, ct);
        var target = cancel ? ProvisionPhase.Cancelled : ProvisionPhase.Ready;
        if (previous is null)
        {
            if (!cancel) throw new ProvisionConflictException("Confirm cannot create a space.");
            await using var tombstone = new NpgsqlCommand("""
                INSERT INTO messaging.channel_provisions(operation_id,channel_id,fingerprint,channel_epoch,routing_generation,phase,decision_version)
                VALUES(@op,@channel,@fingerprint,1,1,2,1)
                """, connection, tx);
            Bind(tombstone, request.OperationId, request.ChannelId, request.Fingerprint);
            await tombstone.ExecuteNonQueryAsync(ct);
        }
        else
        {
            if (previous.Phase != ProvisionPhase.Prepared && previous.Phase != target)
                throw new ProvisionConflictException("The channel already has another terminal decision.");
            if (!cancel)
            {
                await using var server = new NpgsqlCommand("SELECT server_id FROM messaging.channel_provisions WHERE operation_id=@op", connection, tx);
                server.Parameters.AddWithValue("op", request.OperationId);
                var serverId = (Guid)(await server.ExecuteScalarAsync(ct))!;
                await CheckLifecycleAsync(connection, tx, serverId, request.ChannelId, request.ChannelEpoch, ct);
                await CheckSpaceAsync(connection, tx, request.OperationId, request.ChannelId, ct);
            }
            await using var decide = new NpgsqlCommand(cancel ? """
                DELETE FROM messaging.spaces WHERE id=@channel AND provision_operation_id=@op AND provision_state=0;
                UPDATE messaging.channel_provisions SET phase=2,decision_version=1 WHERE operation_id=@op AND phase=0
                """ : """
                UPDATE messaging.spaces SET provision_state=1 WHERE id=@channel AND provision_operation_id=@op AND provision_state=0;
                UPDATE messaging.channel_provisions SET phase=1,decision_version=1 WHERE operation_id=@op AND phase=0
                """, connection, tx);
            decide.Parameters.AddWithValue("op", request.OperationId); decide.Parameters.AddWithValue("channel", request.ChannelId);
            await decide.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
        return new(request.OperationId, request.ChannelId, target, 1);
    }

    public async Task<ProvisionStatus?> GetAsync(Guid operationId, Guid channelId, CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct);
        return await ReadAsync(connection, null, operationId, channelId, null, ct);
    }

    public async Task<ProvisionStatus> ActivateAsync(ChannelReadyPermit permit, CancellationToken ct)
    {
        var identity = permit.Identity; var actor = identity.Request.Actor; var now = clock.GetUtcNow();
        if (permit.Scopes.Select(s => s.Kind).Distinct().Count() != 3)
            throw new ProvisionConflictException("A complete channel permit is required.");
        var member = permit.Scopes.SingleOrDefault(s => s.Kind == 3);
        var access = permit.Scopes.SingleOrDefault(s => s.Kind == 4);
        var lifecycle = permit.Scopes.SingleOrDefault(s => s.Kind == 6);
        Validate(permit.OperationId, permit.ChannelId, permit.Fingerprint, identity.Request.RoutingGeneration, lifecycle?.Epoch ?? 0);
        if (permit.Scopes.Count != 3 || member is null || access is null || lifecycle is null
            || member.UserId != actor.UserId || member.MembershipId == Guid.Empty || member.ChannelId != Guid.Empty
            || access.UserId != actor.UserId || access.MembershipId != member.MembershipId || access.ChannelId != permit.ChannelId
            || lifecycle.UserId != Guid.Empty || lifecycle.MembershipId != Guid.Empty || lifecycle.ChannelId != permit.ChannelId
            || permit.Scopes.Any(s => s.Epoch <= 0 || s.ServerId != permit.ServerId || s.Permission != "")
            || identity.Owner != AccessOwner.Messaging || identity.Request.Purpose != IdentityPurpose.ChannelRead
            || identity.Request.ResourceId != permit.ChannelId || identity.Request.PeerId is not null
            || identity.Request.Context != IdentityAccessContext.HttpCaller || identity.Scopes.Count != 2
            || !identity.Scopes.Select(s => (s.Kind, s.UserId, s.SessionId)).ToHashSet().SetEquals(new[] { ((short)1, actor.UserId, Guid.Empty), ((short)2, actor.UserId, actor.SessionId) })
            || identity.Scopes.Any(s => s.Epoch <= 0) || identity.IssuedAt > now.AddMilliseconds(100)
            || identity.ExpiresAt > identity.IssuedAt.AddSeconds(5) || identity.ExpiresAt > actor.AccessExpiresAt
            || permit.IssuedAt > now.AddMilliseconds(100) || permit.ExpiresAt > permit.IssuedAt.AddSeconds(5)
            || permit.ExpiresAt > identity.ExpiresAt || permit.ExpiresAt - now < TimeSpan.FromMilliseconds(1500))
            throw new ProvisionConflictException("A current channel permit is required.");
        await using var connection = await OpenAsync(ct); await using var tx = await connection.BeginTransactionAsync(ct);
        await GateAsync(connection, tx, permit.OperationId, ct);
        var previous = await ReadAsync(connection, tx, permit.OperationId, permit.ChannelId, permit.Fingerprint, ct);
        if (previous is null || previous.Phase == ProvisionPhase.Cancelled) throw new ProvisionConflictException("The channel was not prepared.");
        await using (var binding = new NpgsqlCommand("SELECT server_id=@server FROM messaging.channel_provisions WHERE operation_id=@op", connection, tx))
        {
            binding.Parameters.AddWithValue("op", permit.OperationId); binding.Parameters.AddWithValue("server", permit.ServerId);
            if (await binding.ExecuteScalarAsync(ct) is not true) throw new ProvisionConflictException("The channel belongs to another server.");
        }
        foreach (var stamp in identity.Scopes.OrderBy(s => s.Kind))
        {
            await using var fence = new NpgsqlCommand("""
                INSERT INTO messaging.access_fences(authority,scope_kind,user_id,session_id,min_epoch) VALUES(1,@kind,@user,@session,1) ON CONFLICT DO NOTHING;
                SELECT min_epoch FROM messaging.access_fences WHERE authority=1 AND scope_kind=@kind AND user_id=@user AND session_id=@session
                AND server_id='00000000-0000-0000-0000-000000000000' AND channel_id='00000000-0000-0000-0000-000000000000'
                AND membership_id='00000000-0000-0000-0000-000000000000' AND permission_code='' FOR SHARE
                """, connection, tx);
            fence.Parameters.AddWithValue("kind", stamp.Kind); fence.Parameters.AddWithValue("user", stamp.UserId); fence.Parameters.AddWithValue("session", stamp.SessionId);
            if (await fence.ExecuteScalarAsync(ct) is not long floor || stamp.Epoch < floor) throw new ProvisionConflictException("Identity access has changed.");
        }
        foreach (var stamp in permit.Scopes.OrderBy(s => s.Kind))
        {
            await using var fence = new NpgsqlCommand("""
                INSERT INTO messaging.access_fences(authority,scope_kind,user_id,server_id,channel_id,membership_id,min_epoch)
                VALUES(2,@kind,@user,@server,@channel,@member,1) ON CONFLICT DO NOTHING;
                SELECT min_epoch FROM messaging.access_fences WHERE authority=2 AND scope_kind=@kind AND user_id=@user AND server_id=@server
                AND channel_id=@channel AND membership_id=@member AND session_id='00000000-0000-0000-0000-000000000000' AND permission_code='' FOR SHARE
                """, connection, tx);
            fence.Parameters.AddWithValue("kind", stamp.Kind); fence.Parameters.AddWithValue("user", stamp.UserId);
            fence.Parameters.AddWithValue("server", stamp.ServerId); fence.Parameters.AddWithValue("channel", stamp.ChannelId); fence.Parameters.AddWithValue("member", stamp.MembershipId);
            if (await fence.ExecuteScalarAsync(ct) is not long floor || stamp.Epoch < floor) throw new ProvisionConflictException("Channel access has changed.");
        }
        await CheckSpaceAsync(connection, tx, permit.OperationId, permit.ChannelId, ct);
        await using (var activate = new NpgsqlCommand("""
            UPDATE messaging.spaces SET provision_state=1 WHERE id=@channel AND provision_operation_id=@op AND provision_state=0;
            UPDATE messaging.channel_provisions SET phase=1,decision_version=1 WHERE operation_id=@op AND phase=0;
            SELECT clock_timestamp()<@expires-interval '250 milliseconds'
            """, connection, tx))
        {
            activate.Parameters.AddWithValue("op", permit.OperationId); activate.Parameters.AddWithValue("channel", permit.ChannelId);
            activate.Parameters.AddWithValue("expires", permit.ExpiresAt);
            if (await activate.ExecuteScalarAsync(ct) is not true) throw new ProvisionConflictException("The channel permit expired.");
        }
        await tx.CommitAsync(ct);
        return new(permit.OperationId, permit.ChannelId, ProvisionPhase.Ready, 1);
    }

    private static async Task GateAsync(NpgsqlConnection connection, NpgsqlTransaction tx, Guid operation, CancellationToken ct)
    {
        await using var gate = new NpgsqlCommand("SET LOCAL lock_timeout='500ms'; SET LOCAL statement_timeout='1s'; SET LOCAL transaction_timeout='1s'; SELECT pg_advisory_xact_lock(1935893327,hashtext(@op))", connection, tx);
        gate.Parameters.AddWithValue("op", operation.ToString("D")); await gate.ExecuteNonQueryAsync(ct);
    }

    private static async Task CheckLifecycleAsync(NpgsqlConnection connection, NpgsqlTransaction tx, Guid server, Guid channel, long epoch, CancellationToken ct)
    {
        await using (var create = new NpgsqlCommand("""
            INSERT INTO messaging.access_fences(authority,scope_kind,server_id,channel_id,min_epoch)
            VALUES(2,6,@server,@channel,1) ON CONFLICT DO NOTHING
            """, connection, tx))
        { create.Parameters.AddWithValue("server", server); create.Parameters.AddWithValue("channel", channel); await create.ExecuteNonQueryAsync(ct); }
        await using var check = new NpgsqlCommand("""
            SELECT min_epoch FROM messaging.access_fences WHERE authority=2 AND scope_kind=6
            AND server_id=@server AND channel_id=@channel AND user_id='00000000-0000-0000-0000-000000000000'
            AND session_id='00000000-0000-0000-0000-000000000000' AND membership_id='00000000-0000-0000-0000-000000000000'
            AND permission_code='' FOR SHARE
            """, connection, tx);
        check.Parameters.AddWithValue("server", server); check.Parameters.AddWithValue("channel", channel);
        if (await check.ExecuteScalarAsync(ct) is not long floor || epoch < floor)
            throw new ProvisionConflictException("The channel lifecycle has advanced.");
    }

    private static async Task CheckSpaceAsync(NpgsqlConnection connection, NpgsqlTransaction tx, Guid operation, Guid channel, CancellationToken ct)
    {
        await using var check = new NpgsqlCommand("SELECT id FROM messaging.spaces WHERE id=@channel AND provision_operation_id=@op AND space_type=3 AND status=1 AND deleted_at IS NULL FOR SHARE", connection, tx);
        check.Parameters.AddWithValue("op", operation); check.Parameters.AddWithValue("channel", channel);
        if (await check.ExecuteScalarAsync(ct) is null) throw new ProvisionConflictException("The provisioned space is no longer available.");
    }

    private static async Task<ProvisionStatus?> ReadAsync(NpgsqlConnection connection, NpgsqlTransaction? tx,
        Guid operation, Guid channel, byte[]? fingerprint, CancellationToken ct)
    {
        await using var read = new NpgsqlCommand("SELECT operation_id,channel_id,fingerprint,phase,decision_version FROM messaging.channel_provisions WHERE operation_id=@op OR channel_id=@channel", connection, tx);
        read.Parameters.AddWithValue("op", operation); read.Parameters.AddWithValue("channel", channel);
        await using var rows = await read.ExecuteReaderAsync(ct);
        if (!await rows.ReadAsync(ct)) return null;
        if (rows.GetGuid(0) != operation || rows.GetGuid(1) != channel
            || fingerprint is not null && !CryptographicOperations.FixedTimeEquals(rows.GetFieldValue<byte[]>(2), fingerprint))
            throw new ProvisionConflictException("The operation key belongs to another payload.");
        return new(operation, channel, (ProvisionPhase)rows.GetInt16(3), rows.GetInt64(4));
    }

    private static void Bind(NpgsqlCommand command, Guid operation, Guid channel, byte[] fingerprint)
    {
        command.Parameters.AddWithValue("op", operation); command.Parameters.AddWithValue("channel", channel);
        command.Parameters.AddWithValue("fingerprint", fingerprint);
    }
    private static void Validate(Guid operation, Guid channel, byte[] fingerprint, long generation, long epoch)
    {
        if (operation == Guid.Empty || channel == Guid.Empty || fingerprint.Length != 32 || generation != 1 || epoch != 1)
            throw new ProvisionConflictException("Invalid channel provisioning request.");
    }
}
