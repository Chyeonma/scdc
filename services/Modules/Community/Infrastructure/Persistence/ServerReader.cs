using System.Text.Json;
using NpgsqlTypes;
using SCDC.BuildingBlocks.Infrastructure.Persistence;
using SCDC.Modules.Community.Features.Servers.Application;
using SCDC.Modules.Community.Infrastructure.Paging;

namespace SCDC.Modules.Community.Infrastructure.Persistence;

internal sealed class ServerReader
{
    internal sealed record SearchMatch(ServerSummary Server, SearchPosition Position);
    private const string MembershipJson = """
        jsonb_build_object('serverId',m.server_id,'userId',m.user_id,'membershipId',m.membership_id,
          'status',CASE m.status WHEN 1 THEN 'active' ELSE 'left' END,'joinedAt',m.joined_at,'leftAt',m.left_at,'version',m.version::text)
        """;
    private const string SummaryJson = """
        jsonb_build_object('id',s.id,'name',s.name,'description',s.description,
          'visibility',CASE s.visibility WHEN 1 THEN 'public' ELSE 'private' END,
          'joinMode',CASE s.join_mode WHEN 1 THEN 'immediate' ELSE 'approval' END,'version',s.version::text)
        """;
    private const string PermissionsJson = """
        CASE WHEN s.owner_user_id=@actor THEN
          '["manage_channels","manage_invites","review_join_requests","manage_join_mode","manage_channel_access"]'::jsonb
        ELSE coalesce((SELECT jsonb_agg(p.code ORDER BY p.code) FROM
          (SELECT DISTINCT rp.permission_code AS code FROM community.member_roles mr
            JOIN community.roles r ON r.id=mr.role_id AND r.server_id=mr.server_id
            JOIN community.role_permissions rp ON rp.role_id=r.id
            WHERE mr.server_id=s.id AND mr.user_id=@actor AND NOT r.is_default AND NOT r.is_system
              AND rp.permission_code IN ('manage_channels','manage_invites','review_join_requests','manage_join_mode','manage_channel_access')) p), '[]'::jsonb) END
        """;
    private static readonly string DetailJson = $"{SummaryJson} || jsonb_build_object('ownerUserId',s.owner_user_id,'accessVersion',s.access_version::text,'myMembership',{MembershipJson},'effectivePermissions',{PermissionsJson})";
    private const string Join = """
        FROM community.servers s LEFT JOIN community.server_members m ON m.server_id=s.id AND m.user_id=@actor
        """;

    public async Task<ServerSummary?> GetAsync(RelationalWorkScope scope, Guid actor, Guid id, CancellationToken ct, bool requireExisting = false)
    {
        await using var query = scope.CreateCommand($"""
            SELECT CASE WHEN s.status=1 AND s.deleted_at IS NULL AND (m.status=1 OR s.visibility=1)
              THEN CASE WHEN m.status=1 THEN {DetailJson} ELSE {SummaryJson} END ELSE NULL END,m.status=1
            {Join} WHERE s.id=@id
            """);
        query.Parameters.AddWithValue("actor", actor);
        query.Parameters.AddWithValue("id", id);
        await using var reader = await query.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            if (requireExisting)
                throw new InvalidOperationException("Operation references a missing server.");
            return null;
        }
        if (reader.IsDBNull(0))
            return null;
        var json = reader.GetString(0);
        return !reader.IsDBNull(1) && reader.GetBoolean(1)
            ? JsonSerializer.Deserialize<ServerDetail>(json, JsonSerializerOptions.Web)!
            : JsonSerializer.Deserialize<ServerSummary>(json, JsonSerializerOptions.Web)!;
    }
    public async Task<List<ServerDetail>> ListAsync(RelationalWorkScope scope, Guid actor, int limit, Guid? lastId, CancellationToken ct)
    {
        await using var query = scope.CreateCommand($"""
            SELECT {DetailJson} {Join}
            WHERE s.status=1 AND s.deleted_at IS NULL AND m.status=1 AND (@lastId IS NULL OR s.id<@lastId)
            ORDER BY s.id DESC LIMIT @take
            """);
        query.Parameters.AddWithValue("actor", actor);
        query.Parameters.AddWithValue("take", limit + 1);
        query.Parameters.AddWithValue("lastId", NpgsqlDbType.Uuid, (object?)lastId ?? DBNull.Value);
        var items = new List<ServerDetail>();
        await using var reader = await query.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            items.Add(JsonSerializer.Deserialize<ServerDetail>(reader.GetString(0), JsonSerializerOptions.Web)!);
        return items;
    }
    public async Task<MembershipView?> MembershipAsync(RelationalWorkScope scope, Guid actor, Guid id, CancellationToken ct)
    {
        await using var query = scope.CreateCommand($"""
            SELECT {MembershipJson} {Join}
            WHERE s.id=@id AND s.status=1 AND s.deleted_at IS NULL AND m.user_id IS NOT NULL
            """);
        query.Parameters.AddWithValue("actor", actor);
        query.Parameters.AddWithValue("id", id);
        var json = await query.ExecuteScalarAsync(ct) as string;
        return json is null ? null : JsonSerializer.Deserialize<MembershipView>(json, JsonSerializerOptions.Web);
    }

    public async Task<List<SearchMatch>> SearchAsync(RelationalWorkScope scope, string key, int limit, SearchPosition? last, CancellationToken ct)
    {
        var pattern = "%" + key.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal) + "%";
        await using var query = scope.CreateCommand($"""
            SELECT {SummaryJson},s.search_name,match.rank FROM community.servers s
            CROSS JOIN LATERAL (SELECT CASE WHEN s.search_name=@key THEN 0 ELSE 1 END AS rank) match
            WHERE s.visibility=1 AND s.status=1 AND s.deleted_at IS NULL AND s.search_name LIKE @pattern ESCAPE '\'
              AND (@lastId IS NULL OR match.rank>@lastRank OR
                (match.rank=@lastRank AND (s.search_name>@lastName OR (s.search_name=@lastName AND s.id>@lastId))))
            ORDER BY match.rank,s.search_name,s.id LIMIT @take
            """);
        query.Parameters.AddWithValue("key", key);
        query.Parameters.AddWithValue("pattern", pattern);
        query.Parameters.AddWithValue("lastId", NpgsqlDbType.Uuid, (object?)last?.Id ?? DBNull.Value);
        query.Parameters.AddWithValue("lastRank", last?.Rank ?? 0);
        query.Parameters.AddWithValue("lastName", last?.Name ?? "");
        query.Parameters.AddWithValue("take", limit + 1);
        var items = new List<SearchMatch>();
        await using var rows = await query.ExecuteReaderAsync(ct);
        while (await rows.ReadAsync(ct))
        {
            var server = JsonSerializer.Deserialize<ServerSummary>(rows.GetString(0), JsonSerializerOptions.Web)!;
            items.Add(new(server, new(rows.GetInt32(2), rows.GetString(1), server.Id)));
        }
        return items;
    }
}
