using System.Globalization;
using System.Text.Json;
using NpgsqlTypes;
using SCDC.BuildingBlocks.Infrastructure.Persistence;
using SCDC.Modules.Community.Features.Permissions.Application;
using SCDC.Modules.Community.Features.Servers.Application;

namespace SCDC.Modules.Community.Infrastructure.Persistence;

internal sealed class RoleReader
{
    private const string RoleJson="""
        jsonb_build_object('id',r.id,'serverId',r.server_id,'name',r.name,'isSystem',r.is_system,'version',r.version::text,
          'permissions',coalesce((SELECT jsonb_agg(permission_code ORDER BY permission_code) FROM community.role_permissions WHERE role_id=r.id),'[]'::jsonb))
        """;
    public async Task<List<RoleView>> ListAsync(RelationalWorkScope scope, Guid server, int limit, Guid? last, CancellationToken ct)
    {
        await using var query=scope.CreateCommand($"SELECT {RoleJson} FROM community.roles r WHERE r.server_id=@server AND(@last IS NULL OR r.id>@last) ORDER BY r.id LIMIT @take");
        query.Parameters.AddWithValue("server",server);query.Parameters.AddWithValue("last",NpgsqlDbType.Uuid,(object?)last??DBNull.Value);query.Parameters.AddWithValue("take",limit+1);
        var results=new List<RoleView>();
        await using var rows=await query.ExecuteReaderAsync(ct);
        while(await rows.ReadAsync(ct))results.Add(JsonSerializer.Deserialize<RoleView>(rows.GetString(0),JsonSerializerOptions.Web)!);
        return results;
    }
    public async Task<RoleView?> GetAsync(RelationalWorkScope scope, Guid server, Guid role, CancellationToken ct)
    {
        await using var query=scope.CreateCommand($"SELECT {RoleJson} FROM community.roles r WHERE r.server_id=@server AND r.id=@role");
        query.Parameters.AddWithValue("server",server);query.Parameters.AddWithValue("role",role);
        var json=await query.ExecuteScalarAsync(ct) as string;
        return json is null ? null : JsonSerializer.Deserialize<RoleView>(json,JsonSerializerOptions.Web);
    }
    public async Task<MemberRoles?> MemberRolesAsync(RelationalWorkScope scope, Guid server, Guid user, CancellationToken ct)
    {
        await using var query=scope.CreateCommand("""
            SELECT m.membership_id,m.version,coalesce((SELECT array_agg(mr.role_id ORDER BY mr.role_id) FROM community.member_roles mr
              JOIN community.roles r ON r.id=mr.role_id AND r.server_id=mr.server_id AND NOT r.is_system
              WHERE mr.server_id=m.server_id AND mr.user_id=m.user_id AND mr.membership_id=m.membership_id),ARRAY[]::uuid[])
            FROM community.server_members m WHERE m.server_id=@server AND m.user_id=@user AND m.status=1
            """);
        query.Parameters.AddWithValue("server",server);query.Parameters.AddWithValue("user",user);
        await using var rows=await query.ExecuteReaderAsync(ct);
        return await rows.ReadAsync(ct)?new(server,user,rows.GetGuid(0),rows.GetInt32(1).ToString(CultureInfo.InvariantCulture),rows.GetFieldValue<Guid[]>(2)):null;
    }
    public async Task<List<MembershipView>> MembersAsync(RelationalWorkScope scope, Guid server, int limit, Guid? last, CancellationToken ct)
    {
        await using var query=scope.CreateCommand($"SELECT {ServerReader.MembershipJson} FROM community.server_members m WHERE m.server_id=@server AND m.status=1 AND(@last IS NULL OR m.user_id>@last) ORDER BY m.user_id LIMIT @take");
        query.Parameters.AddWithValue("server",server);query.Parameters.AddWithValue("last",NpgsqlDbType.Uuid,(object?)last??DBNull.Value);query.Parameters.AddWithValue("take",limit+1);
        var results=new List<MembershipView>();
        await using var rows=await query.ExecuteReaderAsync(ct);
        while(await rows.ReadAsync(ct))results.Add(JsonSerializer.Deserialize<MembershipView>(rows.GetString(0),JsonSerializerOptions.Web)!);
        return results;
    }
}
