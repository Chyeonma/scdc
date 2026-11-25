using Npgsql;
using SCDC.BuildingBlocks.Infrastructure.Persistence;
using SCDC.Contracts.Community;
using SCDC.Contracts.Identity;

namespace SCDC.Modules.Community.Infrastructure.Access;

internal static class CommunityScopeStore
{
    public static string Key(CommunityScope s) => $"{s.Kind}:{s.UserId:D}:{s.ServerId:D}:{s.ChannelId:D}:{s.MembershipId:D}:{s.Permission}";
    public const string Match = "authority=2 AND scope_kind=@kind AND user_id=@user AND server_id=@server AND channel_id=@channel AND membership_id=@member AND permission_code=@permission AND session_id='00000000-0000-0000-0000-000000000000'";
    public static void Bind(NpgsqlCommand command, CommunityScope s)
    {
        command.Parameters.AddWithValue("kind",s.Kind); command.Parameters.AddWithValue("user",s.UserId);
        command.Parameters.AddWithValue("server",s.ServerId); command.Parameters.AddWithValue("channel",s.ChannelId);
        command.Parameters.AddWithValue("member",s.MembershipId); command.Parameters.AddWithValue("permission",s.Permission);
    }
    public static async Task EnsureAsync(RelationalWorkScope scope, CommunityScope s, CancellationToken ct)
    {
        await using var create=scope.CreateCommand("""
            INSERT INTO community.access_scopes(authority,scope_kind,user_id,server_id,channel_id,membership_id,permission_code,current_epoch)
            VALUES(2,@kind,@user,@server,@channel,@member,@permission,1) ON CONFLICT DO NOTHING
            """);
        Bind(create,s); await create.ExecuteNonQueryAsync(ct);
    }
    public static async Task CheckAsync(RelationalWorkScope scope, IEnumerable<CommunityScope> scopes, Guid? acceptedOperation, CancellationToken ct)
    {
        foreach(var s in scopes.OrderBy(Key,StringComparer.Ordinal))
        {
            await EnsureAsync(scope,s,ct);
            await using var query=scope.CreateCommand("SELECT current_epoch,pending_operation_id,retired FROM community.access_scopes WHERE "+Match+" FOR SHARE");
            Bind(query,s);
            await using var rows=await query.ExecuteReaderAsync(ct); await rows.ReadAsync(ct);
            if(rows.GetBoolean(2)) throw new AccessCheckException("ACCESS_CHANGED");
            if(!rows.IsDBNull(1) && rows.GetGuid(1)!=acceptedOperation) throw new AccessCheckException("ACCESS_PENDING");
        }
    }
    public static CommunityScope Membership(Guid actor,Guid server,Guid membership) => new(3,actor,server,Guid.Empty,membership,"",1);
    public static CommunityScope Management(Guid actor,Guid server,Guid membership,string permission) => new(5,actor,server,Guid.Empty,membership,permission,1);
    public static CommunityScope Channel(Guid actor,Guid server,Guid channel,Guid membership) => new(4,actor,server,channel,membership,"",1);
    public static CommunityScope Lifecycle(Guid server,Guid channel) => new(6,Guid.Empty,server,channel,Guid.Empty,"",1);
}
