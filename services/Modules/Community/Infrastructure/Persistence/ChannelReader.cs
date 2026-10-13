using System.Globalization;
using System.Text.Json;
using NpgsqlTypes;
using SCDC.BuildingBlocks.Infrastructure.Persistence;
using SCDC.Modules.Community.Features.Channels.Application;
using SCDC.Modules.Community.Features.Permissions.Domain;
using SCDC.Modules.Community.Features.Permissions.Infrastructure;

namespace SCDC.Modules.Community.Infrastructure.Persistence;

internal sealed record ChannelState(ChannelView View, short DefaultView, int Version, int AccessVersion);
internal sealed class ChannelReader
{
    private const string Projection = """
        jsonb_build_object('id',c.space_id,'serverId',c.server_id,'name',c.name,'topic',c.topic,
            'kind',CASE c.kind WHEN 1 THEN 'text' ELSE 'voice' END,'createdAt',c.created_at,
            'version',c.version::text,'accessVersion',c.access_version::text)
        """;
    // Mirrors the pure evaluator: personal override, role deny-wins, role allow, default. Owner wins after foundation.
    private const string Visible = """
        (@owner OR CASE coalesce((SELECT u.effect FROM community.channel_user_overrides u
          WHERE u.space_id=c.space_id AND u.user_id=@actor AND u.membership_id=@epoch),0)
          WHEN 1 THEN true WHEN 2 THEN false ELSE
            CASE WHEN EXISTS(SELECT 1 FROM community.channel_role_overrides o JOIN community.roles r ON r.id=o.role_id
              WHERE o.space_id=c.space_id AND o.effect=2 AND(r.is_system OR EXISTS(SELECT 1 FROM community.member_roles mr
                WHERE mr.role_id=r.id AND mr.user_id=@actor AND mr.membership_id=@epoch))) THEN false
            WHEN EXISTS(SELECT 1 FROM community.channel_role_overrides o JOIN community.roles r ON r.id=o.role_id
              WHERE o.space_id=c.space_id AND o.effect=1 AND(r.is_system OR EXISTS(SELECT 1 FROM community.member_roles mr
                WHERE mr.role_id=r.id AND mr.user_id=@actor AND mr.membership_id=@epoch))) THEN true
            ELSE c.default_view=1 END END)
        """;
    public async Task<List<ChannelView>> ListAsync(RelationalWorkScope scope, Guid actor, Guid server,
        ManagementLease lease, int limit, Guid? last, CancellationToken ct)
    {
        await using var query = scope.CreateCommand($"""
            SELECT {Projection} FROM community.channels c WHERE c.server_id=@server AND c.status=1 AND c.deleted_at IS NULL
            AND(@last IS NULL OR c.space_id>@last) AND {Visible} ORDER BY c.space_id LIMIT @take
            """);
        query.Parameters.AddWithValue("server", server); query.Parameters.AddWithValue("actor", actor);
        query.Parameters.AddWithValue("owner", lease.IsOwner); query.Parameters.AddWithValue("epoch", lease.MembershipId);
        query.Parameters.AddWithValue("last", NpgsqlDbType.Uuid, (object?)last ?? DBNull.Value); query.Parameters.AddWithValue("take", limit + 1);
        var results = new List<ChannelView>();
        await using var rows = await query.ExecuteReaderAsync(ct);
        while (await rows.ReadAsync(ct)) results.Add(JsonSerializer.Deserialize<ChannelView>(rows.GetString(0), JsonSerializerOptions.Web)!);
        return results;
    }
    public async Task<ChannelState?> StateAsync(RelationalWorkScope scope, Guid server, Guid channel, bool write, CancellationToken ct)
    {
        await using var query = scope.CreateCommand($"""
            SELECT {Projection},c.default_view,c.version,c.access_version FROM community.channels c
            WHERE c.server_id=@server AND c.space_id=@channel AND c.status=1 AND c.deleted_at IS NULL FOR {(write ? "UPDATE" : "SHARE")}
            """);
        query.Parameters.AddWithValue("server", server); query.Parameters.AddWithValue("channel", channel);
        await using var rows = await query.ExecuteReaderAsync(ct);
        return await rows.ReadAsync(ct) ? new(JsonSerializer.Deserialize<ChannelView>(rows.GetString(0), JsonSerializerOptions.Web)!,
            rows.GetInt16(1), rows.GetInt32(2), rows.GetInt32(3)) : null;
    }
    public async Task<PermissionEvaluation> EvaluateAsync(RelationalWorkScope scope, Guid actor, Guid server,
        ChannelState channel, ManagementLease lease, CancellationToken ct)
    {
        var roles = new List<RoleGrant>();
        await using (var query = scope.CreateCommand("""
            SELECT r.is_system,coalesce(o.effect,0)::smallint,coalesce((SELECT array_agg(rp.permission_code)
                FROM community.role_permissions rp WHERE rp.role_id=r.id),ARRAY[]::varchar[])
            FROM community.roles r LEFT JOIN community.channel_role_overrides o ON o.role_id=r.id AND o.space_id=@channel
            WHERE r.server_id=@server AND(r.is_system OR EXISTS(SELECT 1 FROM community.member_roles mr
                WHERE mr.role_id=r.id AND mr.user_id=@actor AND mr.membership_id=@epoch))
            """))
        {
            query.Parameters.AddWithValue("server", server); query.Parameters.AddWithValue("actor", actor);
            query.Parameters.AddWithValue("channel", channel.View.Id); query.Parameters.AddWithValue("epoch", lease.MembershipId);
            await using var rows = await query.ExecuteReaderAsync(ct);
            while (await rows.ReadAsync(ct)) roles.Add(new(ToEffect(rows.GetInt16(1)), rows.GetFieldValue<string[]>(2), rows.GetBoolean(0)));
        }
        await using var personal = scope.CreateCommand("SELECT effect FROM community.channel_user_overrides WHERE space_id=@channel AND user_id=@actor AND membership_id=@epoch");
        personal.Parameters.AddWithValue("channel", channel.View.Id); personal.Parameters.AddWithValue("actor", actor); personal.Parameters.AddWithValue("epoch", lease.MembershipId);
        var value = await personal.ExecuteScalarAsync(ct);
        return PermissionEvaluator.Evaluate(new(true, true, true, true, true, true, lease.IsOwner, channel.View.Kind,
            ToEffect(channel.DefaultView), roles, value is short effect ? ToEffect(effect) : ViewEffect.Inherit));
    }
    public async Task<ChannelAccess> AccessAsync(RelationalWorkScope scope, Guid server, ChannelState channel, CancellationToken ct)
    {
        var roles = new List<RoleOverride>(); var members = new List<MemberOverride>();
        await using (var query = scope.CreateCommand("SELECT role_id,effect FROM community.channel_role_overrides WHERE space_id=@channel ORDER BY role_id"))
        {
            query.Parameters.AddWithValue("channel", channel.View.Id);
            await using var rows = await query.ExecuteReaderAsync(ct);
            while (await rows.ReadAsync(ct)) roles.Add(new(rows.GetGuid(0), rows.GetInt16(1) == 1 ? "allow" : "deny"));
        }
        await using (var query = scope.CreateCommand("SELECT user_id,membership_id,effect FROM community.channel_user_overrides WHERE space_id=@channel ORDER BY user_id"))
        {
            query.Parameters.AddWithValue("channel", channel.View.Id);
            await using var rows = await query.ExecuteReaderAsync(ct);
            while (await rows.ReadAsync(ct)) members.Add(new(rows.GetGuid(0), rows.GetGuid(1), rows.GetInt16(2) == 1 ? "allow" : "deny"));
        }
        return new(server, channel.View.Id, channel.AccessVersion.ToString(CultureInfo.InvariantCulture),
            channel.DefaultView == 1 ? "allow" : "deny", roles, members);
    }
    private static ViewEffect ToEffect(short value) => value switch { 1 => ViewEffect.Allow, 2 => ViewEffect.Deny, _ => ViewEffect.Inherit };
}
