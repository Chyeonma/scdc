using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.BuildingBlocks.Application.Text;
using SCDC.BuildingBlocks.Infrastructure.Outbox;
using SCDC.BuildingBlocks.Infrastructure.Persistence;
using SCDC.Contracts.Identity;
using SCDC.Contracts.Messaging;
using SCDC.Modules.Community.Features.Permissions.Application;
using SCDC.Modules.Community.Features.Permissions.Infrastructure;
using SCDC.Modules.Community.Infrastructure;
using SCDC.Modules.Community.Infrastructure.Idempotency;
using SCDC.Modules.Community.Infrastructure.Paging;
using SCDC.Modules.Community.Infrastructure.Persistence;

namespace SCDC.Modules.Community.Features.Channels.Application;

internal sealed class ChannelService(RelationalWorkScopeFactory scopes, CommunityManagementGuard management,
    CommunityChannelGuard guard, ChannelReader reader, ConfigurationCursorCodec cursors,
    IChatSpaceLifecycle spaces, IOptionsMonitor<CommunityOptions> options, TransactionalOutbox outbox,
    TimeProvider clock, ILogger<ChannelService> logger) : IChannelService
{
    public Task<Result<ChannelPage>> ListAsync(AccountActor actor, Guid server, int limit, string? cursor, CancellationToken ct) => ExecuteAsync(async () =>
    {
        if (limit is < 1 or > 50) throw Invalid("limit", "Limit must be between 1 and 50.");
        var last = cursor is null ? (Guid?)null : cursors.Decode("Channels", cursor, actor.UserId, server, limit);
        await using var scope = await scopes.OpenAsync(ct);
        var lease = await management.AcquireAsync(scope, actor, server, false, false, ct, requiredPermission: null);
        var items = await reader.ListAsync(scope, actor.UserId, server, lease, limit, last, ct);
        string? next = null;
        if (items.Count > limit) { items.RemoveAt(items.Count - 1); next = cursors.Encode("Channels", actor.UserId, server, limit, items[^1].Id); }
        management.EnsureLease(lease.Account); await scope.CommitAsync(ct);
        return new ChannelPage(items, next);
    });
    public Task<Result<ChannelView>> GetAsync(AccountActor actor, Guid server, Guid channel, CancellationToken ct) => ExecuteAsync(async () =>
    {
        await using var scope = await scopes.OpenAsync(ct);
        var lease = await guard.AcquireCoreAsync(scope, actor, server, channel, false, null, ct);
        management.EnsureLease(lease.Server.Account); await scope.CommitAsync(ct);
        return lease.Channel.View;
    });
    public Task<Result<CreateChannelResult>> CreateAsync(AccountActor actor, Guid server, CreateChannelCommand command, CancellationToken ct) => ExecuteAsync(async () =>
    {
        if (command.ClientOperationId.Version != 4 || (command.ClientOperationId.ToByteArray(true)[8] & 0xc0) != 0x80)
            throw Invalid("clientOperationId", "A UUIDv4 with RFC variant is required.");
        var name = Name(command.Name); var topic = Topic(command.Topic);
        if (command.Kind is not (null or "text")) throw Invalid("kind", "This package supports text channels only.");
        await using var scope = await scopes.OpenAsync(ct);
        var lease = await management.AcquireAsync(scope, actor, server, true, false, ct, "manage_channels");
        await using (var query = scope.CreateCommand("SELECT resource_id,fingerprint,key_id,fingerprint_version FROM community.operations WHERE actor_user_id=@actor AND kind='create_channel' AND scope_id=@server AND client_operation_id=@op"))
        {
            query.Parameters.AddWithValue("actor", actor.UserId); query.Parameters.AddWithValue("server", server); query.Parameters.AddWithValue("op", command.ClientOperationId);
            Guid? resource = null; byte[]? fingerprint = null; string? key = null; short version = 1;
            await using (var rows = await query.ExecuteReaderAsync(ct))
                if (await rows.ReadAsync(ct)) { resource = rows.GetGuid(0); fingerprint = rows.GetFieldValue<byte[]>(1); key = rows.GetString(2); version = rows.GetInt16(3); }
            if (resource is Guid existing)
            {
                var hash = OperationFingerprint.ComputeChannel(options.CurrentValue.Operations.GetKey(key!), actor.UserId, server, command.ClientOperationId, name, topic, 1, version);
                if (!CryptographicOperations.FixedTimeEquals(hash, fingerprint!)) throw PermissionFailure.Conflict("OPERATION_CONFLICT", "Operation key was used with different data.");
                var current = await reader.StateAsync(scope, server, existing, false, ct)
                    ?? throw PermissionFailure.Conflict("OPERATION_RESOURCE_REMOVED", "The channel created by this operation was removed.");
                if (!(await reader.EvaluateAsync(scope, actor.UserId, server, current, lease, ct)).CanView) throw PermissionFailure.Missing();
                management.EnsureLease(lease.Account); await scope.CommitAsync(ct);
                return new CreateChannelResult(current.View, false);
            }
        }
        Capacity(lease);
        var id = Guid.CreateVersion7(); var now = clock.GetUtcNow(); var keys = options.CurrentValue.Operations;
        var hashNew = OperationFingerprint.ComputeChannel(keys.GetKey(keys.ActiveKeyId), actor.UserId, server, command.ClientOperationId, name, topic, 1);
        await spaces.CreateChannelAsync(id, actor.UserId, now, scope.Transaction, ct);
        await Write(scope, "INSERT INTO community.channels(space_id,server_id,name,name_key,topic,kind,default_view,created_at,updated_at) VALUES(@id,@server,@name,@key,@topic,1,1,@now,@now)", ct,
            ("id", id), ("server", server), ("name", name), ("key", UnicodeTextPolicy.NormalizeNameKey(name)), ("topic", (object?)topic ?? DBNull.Value), ("now", now));
        await Write(scope, "INSERT INTO community.operations(actor_user_id,kind,scope_id,client_operation_id,fingerprint_version,key_id,fingerprint,resource_id) VALUES(@actor,'create_channel',@server,@op,1,@key,@hash,@id)", ct,
            ("actor", actor.UserId), ("server", server), ("op", command.ClientOperationId), ("key", keys.ActiveKeyId), ("hash", hashNew), ("id", id));
        await Changed(scope, server, id, lease, "channel_created", 1, ct);
        var created = await reader.StateAsync(scope, server, id, false, ct) ?? throw new InvalidOperationException("Created channel missing.");
        management.EnsureLease(lease.Account); await scope.CommitAsync(ct);
        return new CreateChannelResult(created.View, true);
    });
    public Task<Result<ChannelView>> UpdateAsync(AccountActor actor, Guid server, Guid channel, UpdateChannelCommand command, CancellationToken ct) => ExecuteAsync(async () =>
    {
        var expected = Version(command.ExpectedVersion, "expectedVersion");
        if (!command.HasName && !command.HasTopic) throw Invalid("name", "Specify a name or topic to update.");
        var name = command.HasName ? Name(command.Name) : null; var topic = command.HasTopic ? Topic(command.Topic) : null;
        await using var scope = await scopes.OpenAsync(ct);
        var lease = await guard.AcquireCoreAsync(scope, actor, server, channel, true, "manage_channels", ct);
        var current = lease.Channel;
        if (current.Version != expected) throw PermissionFailure.Conflict("VERSION_CONFLICT", "The channel has changed. Reload before saving.");
        name ??= current.View.Name; if (!command.HasTopic) topic = current.View.Topic;
        if (name == current.View.Name && topic == current.View.Topic)
        { management.EnsureLease(lease.Server.Account); await scope.CommitAsync(ct); return current.View; }
        Capacity(lease.Server, current.Version);
        await Write(scope, "UPDATE community.channels SET name=@name,name_key=@key,topic=@topic WHERE space_id=@channel", ct,
            ("name", name), ("key", UnicodeTextPolicy.NormalizeNameKey(name)), ("topic", (object?)topic ?? DBNull.Value), ("channel", channel));
        await Changed(scope, server, channel, lease.Server, "channel_updated", current.AccessVersion, ct);
        var result = await reader.StateAsync(scope, server, channel, false, ct) ?? throw new InvalidOperationException("Updated channel missing.");
        management.EnsureLease(lease.Server.Account); await scope.CommitAsync(ct); return result.View;
    });
    public Task<Result<ChannelAccess>> AccessAsync(AccountActor actor, Guid server, Guid channel, CancellationToken ct) => ExecuteAsync(async () =>
    {
        await using var scope = await scopes.OpenAsync(ct);
        var lease = await guard.AcquireCoreAsync(scope, actor, server, channel, false, "manage_channel_access", ct);
        var result = await reader.AccessAsync(scope, server, lease.Channel, ct);
        management.EnsureLease(lease.Server.Account); await scope.CommitAsync(ct); return result;
    });
    public Task<Result<ChannelAccess>> ReplaceAccessAsync(AccountActor actor, Guid server, Guid channel, ReplaceChannelAccessCommand command, CancellationToken ct) => ExecuteAsync(async () =>
    {
        var expected = Version(command.ExpectedAccessVersion, "expectedAccessVersion");
        if (command.DefaultView is not ("allow" or "deny")) throw Invalid("defaultView", "Choose allow or deny.");
        var roles = command.RoleOverrides; var members = command.MemberOverrides;
        if (roles is null || roles.Count > 21 || roles.Any(r => r is null || r.RoleId == Guid.Empty || r.Effect is not ("allow" or "deny")) || roles.Select(r => r.RoleId).Distinct().Count() != roles.Count)
            throw Invalid("roleOverrides", "Specify distinct roles with allow or deny effects.");
        if (members is null || members.Any(m => m is null || m.UserId == Guid.Empty || m.MembershipId == Guid.Empty || m.Effect is not ("allow" or "deny")) || members.Select(m => m.UserId).Distinct().Count() != members.Count)
            throw Invalid("memberOverrides", "Specify distinct members with an epoch and allow or deny effect.");
        await using var scope = await scopes.OpenAsync(ct);
        var lease = await guard.AcquireCoreAsync(scope, actor, server, channel, true, "manage_channel_access", ct);
        if (lease.Channel.AccessVersion != expected) throw PermissionFailure.Conflict("VERSION_CONFLICT", "The access configuration has changed. Reload before saving.");
        await ValidateTargets(scope, server, roles, members, ct);
        var old = await reader.AccessAsync(scope, server, lease.Channel, ct);
        if (old.DefaultView == command.DefaultView && old.RoleOverrides.SequenceEqual(roles.OrderBy(r => r.RoleId)) && old.MemberOverrides.SequenceEqual(members.OrderBy(m => m.UserId)))
        { management.EnsureLease(lease.Server.Account); await scope.CommitAsync(ct); return old; }
        Capacity(lease.Server, lease.Channel.Version, lease.Channel.AccessVersion);
        await Write(scope, """
            DELETE FROM community.channel_role_overrides WHERE space_id=@channel;
            DELETE FROM community.channel_user_overrides WHERE space_id=@channel;
            INSERT INTO community.channel_role_overrides(space_id,server_id,role_id,permission_code,effect)
                SELECT @channel,@server,role,'channel_view',effect FROM unnest(@roles::uuid[],@roleEffects::smallint[]) AS item(role,effect);
            INSERT INTO community.channel_user_overrides(space_id,server_id,user_id,membership_id,permission_code,effect)
                SELECT @channel,@server,user_id,epoch,'channel_view',effect FROM unnest(@users::uuid[],@epochs::uuid[],@memberEffects::smallint[]) AS item(user_id,epoch,effect);
            UPDATE community.channels SET default_view=@view,access_version=access_version+1 WHERE space_id=@channel;
            """, ct, ("channel", channel), ("server", server), ("roles", roles.Select(r => r.RoleId).ToArray()), ("roleEffects", roles.Select(r => Effect(r.Effect)).ToArray()),
            ("users", members.Select(m => m.UserId).ToArray()), ("epochs", members.Select(m => m.MembershipId).ToArray()), ("memberEffects", members.Select(m => Effect(m.Effect)).ToArray()), ("view", Effect(command.DefaultView)));
        await Changed(scope, server, channel, lease.Server, "channel_access_changed", expected + 1, ct);
        var state = await reader.StateAsync(scope, server, channel, false, ct) ?? throw new InvalidOperationException("Updated channel missing.");
        var result = await reader.AccessAsync(scope, server, state, ct);
        management.EnsureLease(lease.Server.Account); await scope.CommitAsync(ct); return result;
    });
    private static async Task ValidateTargets(RelationalWorkScope scope, Guid server, IReadOnlyList<RoleOverride> roles, IReadOnlyList<MemberOverride> members, CancellationToken ct)
    {
        await using (var query = scope.CreateCommand("SELECT count(*) FROM community.roles WHERE server_id=@server AND id=ANY(@roles)"))
        {
            query.Parameters.AddWithValue("server", server); query.Parameters.AddWithValue("roles", roles.Select(r => r.RoleId).ToArray());
            if ((long)(await query.ExecuteScalarAsync(ct))! != roles.Count) throw Invalid("roleOverrides", "Every role must belong to this community.");
        }
        await using var memberQuery = scope.CreateCommand("SELECT user_id,membership_id,status FROM community.server_members WHERE server_id=@server AND user_id=ANY(@users)");
        memberQuery.Parameters.AddWithValue("server", server); memberQuery.Parameters.AddWithValue("users", members.Select(m => m.UserId).ToArray());
        var targets = members.ToDictionary(m => m.UserId); var count = 0;
        await using var rows = await memberQuery.ExecuteReaderAsync(ct);
        while (await rows.ReadAsync(ct))
        {
            count++;
            if (targets[rows.GetGuid(0)].MembershipId != rows.GetGuid(1)) throw PermissionFailure.Conflict("MEMBERSHIP_CHANGED", "A member has a different membership epoch.");
            if (rows.GetInt16(2) != 1) throw Invalid("memberOverrides", "Every member must be active.");
        }
        if (count != members.Count) throw Invalid("memberOverrides", "Every member must belong to this community.");
    }
    private async Task Changed(RelationalWorkScope scope, Guid server, Guid channel, ManagementLease lease, string cause, int channelAccess, CancellationToken ct)
    {
        await Write(scope, "UPDATE community.servers SET access_version=access_version+1 WHERE id=@server", ct, ("server", server));
        await outbox.AppendAsync(scope, "Community.AccessChanged.v1", "community.server", server, lease.ServerVersion + 1,
            new { serverId = server, channelId = channel, accessVersion = (lease.AccessVersion + 1).ToString(CultureInfo.InvariantCulture),
                channelAccessVersion = channelAccess.ToString(CultureInfo.InvariantCulture), cause }, ct);
    }
    private static void Capacity(ManagementLease lease, int version = 1, int access = 1)
    {
        if (lease.ServerVersion == int.MaxValue || lease.AccessVersion == int.MaxValue || version == int.MaxValue || access == int.MaxValue)
            throw PermissionFailure.Conflict("VERSION_LIMIT_REACHED", "The resource version limit has been reached.");
    }
    private static int Version(string? value, string field)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var result) || result < 1 || value != result.ToString(CultureInfo.InvariantCulture))
            throw Invalid(field, "A positive resource version string is required.");
        return result;
    }
    private static string Name(string? name)
    {
        if (name is null || !UnicodeTextPolicy.IsValidUnicode(name)) throw Invalid("name", "A valid Unicode name is required.");
        name = UnicodeTextPolicy.TrimWhitespace(name);
        if (!UnicodeTextPolicy.IsValidName(name, 1, 100)) throw Invalid("name", "Name must contain 1–100 UTF-16 units of single-line text.");
        return name;
    }
    private static string? Topic(string? topic)
    {
        topic = UnicodeTextPolicy.NormalizeDescription(topic);
        if (topic is not null && (!UnicodeTextPolicy.IsValidUnicode(topic) || topic.Length > 1000)) throw Invalid("topic", "Topic must contain at most 1,000 valid UTF-16 units.");
        return topic;
    }
    private static short Effect(string? value) => (short)(value == "allow" ? 1 : 2);
    private static PermissionFailure Invalid(string field, string message) => new(new ValidationError("VALIDATION_FAILED", "Invalid channel data.", new Dictionary<string, string[]> { { field, [message] } }));
    private static async Task Write(RelationalWorkScope scope, string sql, CancellationToken ct, params (string, object)[] parameters)
    {
        await using var query = scope.CreateCommand(sql);
        foreach (var (key, value) in parameters)
        {
            if (value is DBNull) query.Parameters.AddWithValue(key, NpgsqlTypes.NpgsqlDbType.Text, value);
            else query.Parameters.AddWithValue(key, value);
        }
        await query.ExecuteNonQueryAsync(ct);
    }
    private async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try { return Result.Success(await action()); }
        catch (PermissionFailure ex) { return Result.Failure<T>(ex.Error); }
        catch (FingerprintKeyUnavailableException) { return Result.Failure<T>(Error.ServiceUnavailable("FINGERPRINT_KEY_UNAVAILABLE", "The operation key is unavailable.")); }
        catch (InvalidCursorException) { return Result.Failure<T>(Error.Validation("CURSOR_INVALID", "Cursor is invalid or expired.")); }
        catch (CursorKeyUnavailableException) { return Result.Failure<T>(Error.ServiceUnavailable("CURSOR_KEY_UNAVAILABLE", "Cursor keys are unavailable.")); }
        catch (PostgresException ex) when (ex is { SqlState: "23505", ConstraintName: "ux_channel_active_name_key" })
        { return Result.Failure<T>(Error.Conflict("NAME_CONFLICT", "A channel with this name already exists.")); }
        catch (NpgsqlException ex) when (ex.IsTransient || ex is PostgresException { SqlState: "55P03" or "40P01" or "40001" or "57014" })
        {
            logger.LogWarning("Community channel operation was interrupted ({FailureType}).", ex.GetType().Name);
            return Result.Failure<T>(Error.ServiceUnavailable("COMMUNITY_TEMPORARILY_UNAVAILABLE", "Community is temporarily unavailable. Reload current state before retrying."));
        }
    }
}
