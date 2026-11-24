using SCDC.BuildingBlocks.Infrastructure.Persistence;
using SCDC.Contracts.Community;
using SCDC.Modules.Community.Features.Permissions.Domain;

namespace SCDC.Modules.Community.Infrastructure.Access;

internal sealed class CommunityPolicySnapshot
{
    public Dictionary<string,CommunityScope> Allowed { get; }=[];
    private void Add(CommunityScope s)=>Allowed[CommunityScopeStore.Key(s)]=s;
    public static async Task<CommunityPolicySnapshot> ReadAsync(RelationalWorkScope scope,Guid server,CancellationToken ct)
    {
        var result=new CommunityPolicySnapshot(); Guid owner;
        await using(var query=scope.CreateCommand("SELECT owner_user_id FROM community.servers WHERE id=@server AND status=1 AND deleted_at IS NULL"))
        {query.Parameters.AddWithValue("server",server);if(await query.ExecuteScalarAsync(ct) is not Guid id)return result;owner=id;}
        var members=new List<(Guid User,Guid Membership)>();
        await using(var query=scope.CreateCommand("SELECT user_id,membership_id FROM community.server_members WHERE server_id=@server AND status=1"))
        {query.Parameters.AddWithValue("server",server);await using var rows=await query.ExecuteReaderAsync(ct);while(await rows.ReadAsync(ct))members.Add((rows.GetGuid(0),rows.GetGuid(1)));}
        var roles=new Dictionary<Guid,(bool System,string[] Permissions)>();
        await using(var query=scope.CreateCommand("SELECT id,is_system,coalesce((SELECT array_agg(permission_code::text) FROM community.role_permissions WHERE role_id=r.id),ARRAY[]::text[]) FROM community.roles r WHERE server_id=@server"))
        {query.Parameters.AddWithValue("server",server);await using var rows=await query.ExecuteReaderAsync(ct);while(await rows.ReadAsync(ct))roles[rows.GetGuid(0)]=(rows.GetBoolean(1),rows.GetFieldValue<string[]>(2));}
        var assignments=new HashSet<(Guid User,Guid Membership,Guid Role)>();
        await using(var query=scope.CreateCommand("SELECT user_id,membership_id,role_id FROM community.member_roles WHERE server_id=@server"))
        {query.Parameters.AddWithValue("server",server);await using var rows=await query.ExecuteReaderAsync(ct);while(await rows.ReadAsync(ct))assignments.Add((rows.GetGuid(0),rows.GetGuid(1),rows.GetGuid(2)));}
        var channels=new List<(Guid Id,short Kind,short Default)>();
        await using(var query=scope.CreateCommand("SELECT space_id,kind,default_view FROM community.channels WHERE server_id=@server AND status=1 AND deleted_at IS NULL"))
        {query.Parameters.AddWithValue("server",server);await using var rows=await query.ExecuteReaderAsync(ct);while(await rows.ReadAsync(ct))channels.Add((rows.GetGuid(0),rows.GetInt16(1),rows.GetInt16(2)));}
        var roleViews=new Dictionary<(Guid Channel,Guid Role),ViewEffect>();
        await using(var query=scope.CreateCommand("SELECT space_id,role_id,effect FROM community.channel_role_overrides WHERE server_id=@server"))
        {query.Parameters.AddWithValue("server",server);await using var rows=await query.ExecuteReaderAsync(ct);while(await rows.ReadAsync(ct))roleViews[(rows.GetGuid(0),rows.GetGuid(1))]=Effect(rows.GetInt16(2));}
        var userViews=new Dictionary<(Guid Channel,Guid User,Guid Membership),ViewEffect>();
        await using(var query=scope.CreateCommand("SELECT space_id,user_id,membership_id,effect FROM community.channel_user_overrides WHERE server_id=@server"))
        {query.Parameters.AddWithValue("server",server);await using var rows=await query.ExecuteReaderAsync(ct);while(await rows.ReadAsync(ct))userViews[(rows.GetGuid(0),rows.GetGuid(1),rows.GetGuid(2))]=Effect(rows.GetInt16(3));}
        foreach(var channel in channels)result.Add(CommunityScopeStore.Lifecycle(server,channel.Id));
        foreach(var member in members)
        {
            result.Add(CommunityScopeStore.Membership(member.User,server,member.Membership));
            var assigned=roles.Where(r=>r.Value.System||assignments.Contains((member.User,member.Membership,r.Key))).ToArray();
            var management=PermissionEvaluator.Management(true,owner==member.User,assigned.Select(r=>new RoleGrant(ViewEffect.Inherit,r.Value.Permissions,r.Value.System)));
            foreach(var permission in management)result.Add(CommunityScopeStore.Management(member.User,server,member.Membership,permission));
            if(owner==member.User)result.Add(CommunityScopeStore.Management(member.User,server,member.Membership,"server_owner"));
            foreach(var channel in channels)
            {
                var grants=assigned.Select(r=>new RoleGrant(roleViews.GetValueOrDefault((channel.Id,r.Key)),r.Value.Permissions,r.Value.System)).ToArray();
                var evaluation=PermissionEvaluator.Evaluate(new(true,true,true,true,true,true,owner==member.User,channel.Kind==1?"text":"voice",Effect(channel.Default),grants,userViews.GetValueOrDefault((channel.Id,member.User,member.Membership))));
                if(evaluation.CanView)result.Add(CommunityScopeStore.Channel(member.User,server,channel.Id,member.Membership));
            }
        }
        return result;
    }
    private static ViewEffect Effect(short effect)=>effect switch{1=>ViewEffect.Allow,2=>ViewEffect.Deny,_=>ViewEffect.Inherit};
}
