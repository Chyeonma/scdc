using System.Globalization;
using Microsoft.Extensions.Logging;
using Npgsql;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Modules.Community.Infrastructure.Outbox;
using SCDC.Modules.Community.Infrastructure.Persistence;
using SCDC.Contracts.Identity;
using SCDC.Modules.Community.Infrastructure.Access;
using SCDC.Modules.Community.Features.Servers.Application;

namespace SCDC.Modules.Community.Features.Memberships.Application;

internal sealed class MembershipService(RelationalWorkScopeFactory scopes, CommunityIdentityAccess access,
    ServerReader reader, TransactionalOutbox outbox, TimeProvider clock, ILogger<MembershipService> logger) : IMembershipService
{
    private sealed class RequestFailure(Error error) : Exception
    {
        public Error Error { get; } = error;
    }

    public async Task<Result<MembershipView>> JoinAsync(AccountActor actor, Guid serverId, CancellationToken ct)
    {
        try
        {
            return Result.Success(await JoinOnceAsync(actor, serverId, ct));
        }
        catch (RequestFailure ex) { return Result.Failure<MembershipView>(ex.Error); }
        catch (NpgsqlException ex) when (ex.IsTransient || ex is PostgresException { SqlState: "55P03" or "40P01" or "40001" or "57014" or "25P04" })
        {
            logger.LogWarning("Community join was interrupted ({FailureType}).", ex.GetType().Name);
            return Result.Failure<MembershipView>(Error.ServiceUnavailable("COMMUNITY_TEMPORARILY_UNAVAILABLE",
                "Community is temporarily unavailable. Check your current membership before trying again."));
        }
    }

    private async Task<MembershipView> JoinOnceAsync(AccountActor actor, Guid serverId, CancellationToken ct)
    {
        var permit = await access.AcquireAsync(actor, IdentityPurpose.CommunityWrite, serverId, Guid.NewGuid(), ct);
        await using var scope = await scopes.OpenAsync(ct);
        var lease = await access.AdmitAsync(scope, permit, ct);

        short joinMode;
        int serverVersion, accessVersion;
        await using (var query = scope.CreateCommand("""
            SELECT status,visibility,join_mode,version,access_version,deleted_at IS NOT NULL
            FROM community.servers WHERE id=@id FOR UPDATE
            """))
        {
            query.Parameters.AddWithValue("id", serverId);
            await using var rows = await query.ExecuteReaderAsync(ct);
            if (!await rows.ReadAsync(ct) || rows.GetInt16(0) != 1 || rows.GetInt16(1) != 1 || rows.GetBoolean(5))
                throw new RequestFailure(Error.NotFound("RESOURCE_NOT_FOUND", "Resource was not found."));
            joinMode = rows.GetInt16(2);
            serverVersion = rows.GetInt32(3);
            accessVersion = rows.GetInt32(4);
        }
        await using(var pending=scope.CreateCommand("SELECT EXISTS(SELECT 1 FROM community.access_changes WHERE server_id=@server AND phase=0)"))
        {pending.Parameters.AddWithValue("server",serverId);if(await pending.ExecuteScalarAsync(ct) is true)throw new AccessCheckException("ACCESS_PENDING");}
        var existing = await reader.MembershipAsync(scope, actor.UserId, serverId, ct);
        if (existing?.Status == "active")
        {
            await access.EnsureAsync(scope, lease, ct);
            await scope.CommitAsync(ct);
            return existing;
        }
        if (joinMode != 1)
            throw new RequestFailure(Error.Conflict("JOIN_APPROVAL_REQUIRED", "This community requires approval to join."));
        if (serverVersion == int.MaxValue || accessVersion == int.MaxValue || existing?.Version == int.MaxValue.ToString(CultureInfo.InvariantCulture))
            throw new RequestFailure(Error.Conflict("VERSION_LIMIT_REACHED", "The resource version limit has been reached."));

        var membershipId = Guid.CreateVersion7();
        if (existing is not null)
        {
            if (!await ChannelAccessMaintenance.BumpAsync(scope, serverId, actor.UserId, false, ct))
                throw new RequestFailure(Error.Conflict("VERSION_LIMIT_REACHED", "A channel access version limit has been reached."));
            await using (var retire = scope.CreateCommand("""
                UPDATE community.access_scopes SET retired=true,version=version+1
                WHERE server_id=@server AND user_id=@actor AND membership_id=@previous AND NOT retired
                """))
            {
                retire.Parameters.AddWithValue("server", serverId);
                retire.Parameters.AddWithValue("actor", actor.UserId);
                retire.Parameters.AddWithValue("previous", existing.MembershipId);
                await retire.ExecuteNonQueryAsync(ct);
            }
            // Remove grants/overrides of the departing epoch before changing it.
            await using var clear = scope.CreateCommand("""
                DELETE FROM community.member_roles WHERE server_id=@id AND user_id=@actor;
                DELETE FROM community.channel_user_overrides WHERE server_id=@id AND user_id=@actor;
                """);
            clear.Parameters.AddWithValue("id", serverId);
            clear.Parameters.AddWithValue("actor", actor.UserId);
            await clear.ExecuteNonQueryAsync(ct);
        }
        await using (var write = scope.CreateCommand(existing is null ? """
            INSERT INTO community.server_members(server_id,user_id,membership_id,joined_at)
            VALUES(@id,@actor,@membership,@now)
            """ : """
            UPDATE community.server_members SET status=1,membership_id=@membership,version=version+1,
              joined_at=@now,left_at=NULL,nickname=NULL,timeout_until=NULL,invited_by_user_id=NULL
            WHERE server_id=@id AND user_id=@actor AND status=2 AND membership_id=@previous
            """))
        {
            write.Parameters.AddWithValue("id", serverId);
            write.Parameters.AddWithValue("actor", actor.UserId);
            write.Parameters.AddWithValue("membership", membershipId);
            write.Parameters.AddWithValue("now", clock.GetUtcNow());
            if (existing is not null) write.Parameters.AddWithValue("previous", existing.MembershipId);
            if (await write.ExecuteNonQueryAsync(ct) != 1)
                throw new InvalidOperationException("Membership changed outside the server mutation lock.");
        }
        await CommunityScopeStore.EnsureAsync(scope, CommunityScopeStore.Membership(actor.UserId, serverId, membershipId), ct);
        await using (var bump = scope.CreateCommand("""
            UPDATE community.servers SET access_version=access_version+1 WHERE id=@id RETURNING version,access_version
            """))
        {
            bump.Parameters.AddWithValue("id", serverId);
            await using var rows = await bump.ExecuteReaderAsync(ct);
            if (!await rows.ReadAsync(ct)) throw new InvalidOperationException("Locked server was not found.");
            serverVersion = rows.GetInt32(0);
            accessVersion = rows.GetInt32(1);
        }
        var membership = await reader.MembershipAsync(scope, actor.UserId, serverId, ct)
            ?? throw new InvalidOperationException("Joined membership was not found.");
        await outbox.AppendAsync(scope, "Community.MembershipJoined.v1", "community.server", serverId, serverVersion,
            new
            {
                serverId,
                userId = actor.UserId,
                membershipId,
                membershipVersion = membership.Version,
                accessVersion = accessVersion.ToString(CultureInfo.InvariantCulture)
            }, ct);
        await access.EnsureAsync(scope, lease, ct);
        await scope.CommitAsync(ct);
        return membership;
    }

}
