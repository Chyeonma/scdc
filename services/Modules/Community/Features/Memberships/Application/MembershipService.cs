using System.Globalization;
using Microsoft.Extensions.Logging;
using Npgsql;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.BuildingBlocks.Infrastructure.Outbox;
using SCDC.BuildingBlocks.Infrastructure.Persistence;
using SCDC.Contracts.Identity;
using SCDC.Modules.Community.Features.Servers.Application;
using SCDC.Modules.Community.Infrastructure.Persistence;

namespace SCDC.Modules.Community.Features.Memberships.Application;

public interface IMembershipService
{
    Task<Result<MembershipView>> JoinAsync(AccountActor actor, Guid serverId, CancellationToken cancellationToken);
}

internal sealed class MembershipService(RelationalWorkScopeFactory scopes, IAccountAccessGuard guard,
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
        catch (NpgsqlException ex) when (ex.IsTransient || ex is PostgresException { SqlState: "55P03" or "40P01" or "40001" or "57014" })
        {
            logger.LogWarning("Community join was interrupted ({FailureType}).", ex.GetType().Name);
            return Result.Failure<MembershipView>(Error.ServiceUnavailable("COMMUNITY_TEMPORARILY_UNAVAILABLE",
                "Community is temporarily unavailable. Check your current membership before trying again."));
        }
    }

    private async Task<MembershipView> JoinOnceAsync(AccountActor actor, Guid serverId, CancellationToken ct)
    {
        await using var scope = await scopes.OpenAsync(ct);
        AccountAccessCheck lease;
        try { lease = await guard.AcquireAsync(actor, scope.Transaction, ct); }
        catch (NpgsqlException) { throw new RequestFailure(Error.ServiceUnavailable("ACCESS_CHECK_UNAVAILABLE", "Account access could not be checked.")); }
        if (lease.Status == AccountAccessStatus.AccountDenied)
            throw new RequestFailure(Error.Forbidden("ACCOUNT_ACCESS_DENIED", "An active account with verified primary email is required."));
        EnsureLease(lease);

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
        var existing = await reader.MembershipAsync(scope, actor.UserId, serverId, ct);
        if (existing?.Status == "active")
        {
            EnsureLease(lease);
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
            // Assignments in the current schema are user-bound. Clear them before changing the epoch.
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
        EnsureLease(lease);
        await scope.CommitAsync(ct);
        return membership;
    }

    private void EnsureLease(AccountAccessCheck lease)
    {
        if (!lease.IsAllowedAt(clock.GetUtcNow()))
            throw new RequestFailure(Error.Unauthorized("SESSION_INVALID", "The session is no longer valid."));
    }
}
