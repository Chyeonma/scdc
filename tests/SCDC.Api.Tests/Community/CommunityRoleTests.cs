using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace SCDC.Api.Tests.Community;

public sealed partial class CommunityApiTests
{
    private Task<HttpResponseMessage> RoleCreateAsync(Guid server,string name="Moderators",string[]? permissions=null,Guid? operation=null,Actor? actor=null,HttpClient? client=null)
        =>SendAsync(HttpMethod.Post,$"/api/v1/servers/{server}/roles",actor??_owner,new{clientOperationId=operation??Guid.NewGuid(),name,permissions=permissions??[]},client);
    private Task<HttpResponseMessage> RoleListAsync(Guid server,Actor? actor=null,int limit=20,string? cursor=null)
        =>SendAsync(HttpMethod.Get,$"/api/v1/servers/{server}/roles?limit={limit}"+(cursor is null?"":"&cursor="+Uri.EscapeDataString(cursor)),actor??_owner);
    private Task<HttpResponseMessage> RoleSetAsync(Guid server,JsonElement membership,Guid[] roles,Actor? actor=null,HttpClient? client=null)
        =>SendAsync(HttpMethod.Put,$"/api/v1/servers/{server}/members/{membership.GetProperty("userId").GetGuid()}/roles",actor??_owner,
            new{membershipId=membership.GetProperty("membershipId").GetGuid(),expectedVersion=membership.GetProperty("version").GetString(),roleIds=roles},client);
    private Task<HttpResponseMessage> MemberRoleGetAsync(Guid server,Actor? target=null)
        =>SendAsync(HttpMethod.Get,$"/api/v1/servers/{server}/members/{(target??_other).UserId}/roles",_owner);
    private Task<object?> AccessEventsAsync(Guid server)=>SqlAsync("SELECT count(*) FROM integration.outbox_events WHERE aggregate_id=@id AND event_type='Community.AccessChanged.v1'",("id",server));
    [Fact]
    public async Task Roles_crud_assignment_union_noops_and_delete_update_current_permissions_atomically()
    {
        var server=(await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        var membership=await JsonAsync(await JoinAsync(server));
        var operation=Guid.NewGuid();
        var a=await JsonAsync(await RoleCreateAsync(server," Điều phối ",["manage_invites","manage_channels"],operation));
        var b=await JsonAsync(await RoleCreateAsync(server,"Quyền xem",["manage_channel_access"]));
        var id=a.GetProperty("id").GetGuid();var bId=b.GetProperty("id").GetGuid();
        Assert.Equal("Điều phối",a.GetProperty("name").GetString());
        var set=await JsonAsync(await RoleSetAsync(server,membership,[bId,id]));
        Assert.Equal("2",set.GetProperty("version").GetString());
        var detail=await JsonAsync(await SendAsync(HttpMethod.Get,$"/api/v1/servers/{server}",_other));
        Assert.Equal(new[]{"manage_channel_access","manage_channels","manage_invites"},detail.GetProperty("effectivePermissions").EnumerateArray().Select(p=>p.GetString()));
        var events=await AccessEventsAsync(server);
        Assert.Equal(HttpStatusCode.OK,(await RoleSetAsync(server,set,[id,bId])).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await SendAsync(HttpMethod.Patch,$"/api/v1/servers/{server}/roles/{id}",_owner,new{expectedVersion="1",permissions=new[]{"manage_channels","manage_invites"}})).StatusCode);
        Assert.Equal(events,await AccessEventsAsync(server));
        var updated=await JsonAsync(await SendAsync(HttpMethod.Patch,$"/api/v1/servers/{server}/roles/{id}",_owner,new{expectedVersion="1",name="Mời",permissions=new[]{"manage_invites"}}));
        Assert.Equal("2",updated.GetProperty("version").GetString());
        var replay=await RoleCreateAsync(server,"Điều phối",["manage_channels","manage_invites"],operation);
        Assert.Equal(HttpStatusCode.OK,replay.StatusCode);
        Assert.Equal("Mời",(await JsonAsync(replay)).GetProperty("name").GetString());
        Assert.Equal(HttpStatusCode.NoContent,(await SendAsync(HttpMethod.Delete,$"/api/v1/servers/{server}/roles/{id}?expectedVersion=2",_owner)).StatusCode);
        var after=await JsonAsync(await MemberRoleGetAsync(server));
        Assert.Equal("3",after.GetProperty("version").GetString());
        Assert.Equal(new[]{bId},after.GetProperty("roleIds").EnumerateArray().Select(p=>p.GetGuid()));
        await AssertErrorAsync(await RoleSetAsync(server,set,[bId]),HttpStatusCode.Conflict,"VERSION_CONFLICT");
        await AssertErrorAsync(await RoleCreateAsync(server,"Điều phối",["manage_channels","manage_invites"],operation),HttpStatusCode.Conflict,"OPERATION_RESOURCE_REMOVED");
        var final=await JsonAsync(await SendAsync(HttpMethod.Get,$"/api/v1/servers/{server}",_other));
        Assert.Equal(new[]{"manage_channel_access"},final.GetProperty("effectivePermissions").EnumerateArray().Select(p=>p.GetString()));
        Assert.Equal(5L,await AccessEventsAsync(server));
        Assert.Equal(0L,await SqlAsync("SELECT count(*) FROM integration.outbox_events WHERE aggregate_id=@id AND event_type='Community.AccessChanged.v1' AND published_at IS NOT NULL",("id",server)));
    }
    [Fact]
    public async Task Read_catalog_permission_never_grants_role_writer_or_exposes_roster_email()
    {
        var server=(await JsonAsync(await CreateAsync(visibility:"private"))).GetProperty("id").GetGuid();
        await AssertErrorAsync(await RoleListAsync(server,_other),HttpStatusCode.NotFound,"RESOURCE_NOT_FOUND");
        await SqlAsync("UPDATE community.servers SET visibility=1 WHERE id=@id",("id",server));
        var membership=await JsonAsync(await JoinAsync(server));
        await AssertErrorAsync(await RoleListAsync(server,_other),HttpStatusCode.Forbidden,"PERMISSION_DENIED");
        var role=await JsonAsync(await RoleCreateAsync(server,"Reader",["manage_channel_access","manage_channels"]));
        var id=role.GetProperty("id").GetGuid();
        (await RoleSetAsync(server,membership,[id])).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK,(await RoleListAsync(server,_other)).StatusCode);
        var roster=await SendAsync(HttpMethod.Get,$"/api/v1/servers/{server}/members",_other);
        Assert.Equal(HttpStatusCode.OK,roster.StatusCode);
        var text=await roster.Content.ReadAsStringAsync();Assert.DoesNotContain("email",text,StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2,(await JsonAsync(roster)).GetProperty("items").GetArrayLength());
        await AssertErrorAsync(await RoleCreateAsync(server,actor:_other),HttpStatusCode.Forbidden,"PERMISSION_DENIED");
        await AssertErrorAsync(await SendAsync(HttpMethod.Patch,$"/api/v1/servers/{server}/roles/{id}",_other,new{expectedVersion="1",name="Hijacked"}),HttpStatusCode.Forbidden,"PERMISSION_DENIED");
        await AssertErrorAsync(await SendAsync(HttpMethod.Delete,$"/api/v1/servers/{server}/roles/{id}?expectedVersion=1",_other),HttpStatusCode.Forbidden,"PERMISSION_DENIED");
        await AssertErrorAsync(await RoleSetAsync(server,membership,[],_other),HttpStatusCode.Forbidden,"PERMISSION_DENIED");
        await AssertErrorAsync(await SendAsync(HttpMethod.Get,$"/api/v1/servers/{server}/members/{_other.UserId}/roles",_other),HttpStatusCode.Forbidden,"PERMISSION_DENIED");
        var owner=await JsonAsync(await MemberRoleGetAsync(server,_owner));
        (await RoleSetAsync(server,owner,[])).EnsureSuccessStatusCode();
        Assert.Equal(5,(await JsonAsync(await SendAsync(HttpMethod.Get,$"/api/v1/servers/{server}",_owner))).GetProperty("effectivePermissions").GetArrayLength());
    }
    [Fact]
    public async Task System_cross_server_duplicate_and_invalid_role_sets_are_rejected_without_partial_writes()
    {
        var server=(await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        var otherServer=(await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        var system=(await JsonAsync(await RoleListAsync(server))).GetProperty("items")[0].GetProperty("id").GetGuid();
        var foreign=(await JsonAsync(await RoleCreateAsync(otherServer))).GetProperty("id").GetGuid();
        var role=(await JsonAsync(await RoleCreateAsync(server))).GetProperty("id").GetGuid();
        var membership=await JsonAsync(await JoinAsync(server));
        await AssertErrorAsync(await SendAsync(HttpMethod.Patch,$"/api/v1/servers/{server}/roles/{system}",_owner,new{expectedVersion="1",name="Changed"}),HttpStatusCode.Conflict,"SYSTEM_ROLE_IMMUTABLE");
        await AssertErrorAsync(await SendAsync(HttpMethod.Delete,$"/api/v1/servers/{server}/roles/{system}?expectedVersion=1",_owner),HttpStatusCode.Conflict,"SYSTEM_ROLE_IMMUTABLE");
        await AssertErrorAsync(await RoleSetAsync(server,membership,[system]),HttpStatusCode.Conflict,"SYSTEM_ROLE_IMMUTABLE");
        foreach(var roles in new[]{new[]{foreign},new[]{role,role},new[]{role,Guid.Empty}})
            await AssertErrorAsync(await RoleSetAsync(server,membership,roles),HttpStatusCode.BadRequest,"VALIDATION_FAILED");
        await AssertErrorAsync(await SendAsync(HttpMethod.Patch,$"/api/v1/servers/{server}/roles/{foreign}",_owner,new{expectedVersion="1",name="Wrong server"}),HttpStatusCode.NotFound,"RESOURCE_NOT_FOUND");
        await AssertErrorAsync(await SendAsync(HttpMethod.Post,$"/api/v1/servers/{server}/roles",_owner,new{clientOperationId=Guid.NewGuid(),name="Injected",isSystem=true}),HttpStatusCode.BadRequest,"Common.ValidationFailed");
        Assert.Empty((await JsonAsync(await MemberRoleGetAsync(server))).GetProperty("roleIds").EnumerateArray());
        Assert.Equal("1",(await JsonAsync(await MemberRoleGetAsync(server))).GetProperty("version").GetString());
    }
    [Theory]
    [InlineData("")]
    [InlineData("\u200b\u034f")]
    [InlineData("OK\nX")]
    [InlineData("OK\0")]
    public async Task Role_name_invalid_text_is_validation_failure(string name)
    {
        var server=(await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        await AssertErrorAsync(await RoleCreateAsync(server,name),HttpStatusCode.BadRequest,"VALIDATION_FAILED");
        Assert.Equal(0L,await AccessEventsAsync(server));
    }
    [Fact]
    public async Task Role_name_utf16_limits_nfc_case_collision_and_catalog_are_enforced()
    {
        var server=(await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Created,(await RoleCreateAsync(server,string.Concat(Enumerable.Repeat("😀",32)))).StatusCode);
        await AssertErrorAsync(await RoleCreateAsync(server,new string('x',65)),HttpStatusCode.BadRequest,"VALIDATION_FAILED");
        Assert.Equal(HttpStatusCode.Created,(await RoleCreateAsync(server,"A")).StatusCode);
        Assert.Equal(HttpStatusCode.Created,(await RoleCreateAsync(server,"CAFÉ")).StatusCode);
        await AssertErrorAsync(await RoleCreateAsync(server,"Café"),HttpStatusCode.Conflict,"NAME_CONFLICT");
        Assert.Equal(HttpStatusCode.Created,(await RoleCreateAsync(server,"Cafe")).StatusCode);
        await AssertErrorAsync(await RoleCreateAsync(server,"@EVERYONE"),HttpStatusCode.Conflict,"NAME_CONFLICT");
        foreach(var permissions in new[]{new[]{"manage_roles"},new[]{"manage_invites","manage_invites"},new[]{"channel_view"}})
            await AssertErrorAsync(await RoleCreateAsync(server,"Bad permissions",permissions),HttpStatusCode.BadRequest,"VALIDATION_FAILED");
        var id=(await JsonAsync(await RoleListAsync(server))).GetProperty("items").EnumerateArray().First(item=>!item.GetProperty("isSystem").GetBoolean()).GetProperty("id").GetGuid();
        foreach(var version in new string?[]{null,"0","01","-1","2147483648"})
            await AssertErrorAsync(await SendAsync(HttpMethod.Patch,$"/api/v1/servers/{server}/roles/{id}",_owner,new{expectedVersion=version,name="Changed"}),HttpStatusCode.BadRequest,"VALIDATION_FAILED");
        await AssertErrorAsync(await SendAsync(HttpMethod.Patch,$"/api/v1/servers/{server}/roles/{id}",_owner,new{expectedVersion="1",name=(string?)null}),HttpStatusCode.BadRequest,"VALIDATION_FAILED");
    }
    [Fact]
    public async Task Role_and_member_pages_bind_actor_server_limit_purpose_and_restart()
    {
        var server=(await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        for(var i=0;i<3;i++)(await RoleCreateAsync(server,"Role "+i)).EnsureSuccessStatusCode();
        (await JoinAsync(server)).EnsureSuccessStatusCode();
        var first=await JsonAsync(await RoleListAsync(server,limit:2));var cursor=first.GetProperty("nextCursor").GetString()!;
        await using var restarted=new SCDC.Api.Tests.Infrastructure.SCDCWebApplicationFactory();using var client=restarted.CreateClient();
        var next=await JsonAsync(await SendAsync(HttpMethod.Get,$"/api/v1/servers/{server}/roles?limit=2&cursor="+Uri.EscapeDataString(cursor),_owner,client:client));
        Assert.Equal(4,first.GetProperty("items").EnumerateArray().Concat(next.GetProperty("items").EnumerateArray()).Select(p=>p.GetProperty("id").GetGuid()).Distinct().Count());
        Assert.Equal(JsonValueKind.Null,next.GetProperty("nextCursor").ValueKind);
        await AssertErrorAsync(await RoleListAsync(server,_other,2,cursor),HttpStatusCode.BadRequest,"CURSOR_INVALID");
        await AssertErrorAsync(await RoleListAsync(server,limit:1,cursor:cursor),HttpStatusCode.BadRequest,"CURSOR_INVALID");
        await AssertErrorAsync(await RoleListAsync(Guid.CreateVersion7(),limit:2,cursor:cursor),HttpStatusCode.BadRequest,"CURSOR_INVALID");
        await AssertErrorAsync(await SendAsync(HttpMethod.Get,$"/api/v1/servers/{server}/members?limit=2&cursor="+Uri.EscapeDataString(cursor),_owner),HttpStatusCode.BadRequest,"CURSOR_INVALID");
        var members=await JsonAsync(await SendAsync(HttpMethod.Get,$"/api/v1/servers/{server}/members?limit=1",_owner));
        var memberNext=await JsonAsync(await SendAsync(HttpMethod.Get,$"/api/v1/servers/{server}/members?limit=1&cursor="+Uri.EscapeDataString(members.GetProperty("nextCursor").GetString()!),_owner));
        Assert.NotEqual(members.GetProperty("items")[0].GetProperty("membership").GetProperty("userId").GetGuid(),memberNext.GetProperty("items")[0].GetProperty("membership").GetProperty("userId").GetGuid());
        await SqlAsync("UPDATE identity.users SET status=2 WHERE id=@id",("id",_other.UserId));
        var roster=await JsonAsync(await SendAsync(HttpMethod.Get,$"/api/v1/servers/{server}/members",_owner));
        Assert.Equal(JsonValueKind.Null,roster.GetProperty("items").EnumerateArray().Single(p=>p.GetProperty("membership").GetProperty("userId").GetGuid()==_other.UserId).GetProperty("user").ValueKind);
    }
    [Fact]
    public async Task Assignment_cas_and_epoch_foreign_keys_stop_requests_from_before_rejoin()
    {
        var server=(await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        var role=(await JsonAsync(await RoleCreateAsync(server))).GetProperty("id").GetGuid();
        var old=await JsonAsync(await JoinAsync(server));
        var assigned=await JsonAsync(await RoleSetAsync(server,old,[role]));
        await AssertErrorAsync(await RoleSetAsync(server,old,[]),HttpStatusCode.Conflict,"VERSION_CONFLICT");
        await SqlAsync("UPDATE community.server_members SET status=2,left_at=clock_timestamp() WHERE server_id=@server AND user_id=@user",("server",server),("user",_other.UserId));
        var rejoin=await JsonAsync(await JoinAsync(server));
        await AssertErrorAsync(await RoleSetAsync(server,assigned,[role]),HttpStatusCode.Conflict,"MEMBERSHIP_CHANGED");
        var error=await Assert.ThrowsAsync<PostgresException>(()=>SqlAsync("INSERT INTO community.member_roles(server_id,user_id,role_id,membership_id) VALUES(@server,@user,@role,@epoch)",("server",server),("user",_other.UserId),("role",role),("epoch",old.GetProperty("membershipId").GetGuid())));
        Assert.Equal("23503",error.SqlState);
        Assert.Empty((await JsonAsync(await MemberRoleGetAsync(server))).GetProperty("roleIds").EnumerateArray());
        Assert.Equal(rejoin.GetProperty("membershipId").GetGuid(),(await JsonAsync(await MemberRoleGetAsync(server))).GetProperty("membershipId").GetGuid());
    }
    private async Task<DatabaseGate> AccessGateAsync(Guid server)
    {
        var suffix=Guid.NewGuid().ToString("N");var trigger="test_access_"+suffix;var function="test_access_gate_"+suffix;
        var key=System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000,int.MaxValue);
        await SqlAsync($"CREATE FUNCTION integration.{function}() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN IF NEW.event_type='Community.AccessChanged.v1' AND NEW.aggregate_id='{server}' THEN PERFORM pg_advisory_xact_lock({key}::bigint); END IF; RETURN NEW; END $$; CREATE TRIGGER {trigger} BEFORE INSERT ON integration.outbox_events FOR EACH ROW EXECUTE FUNCTION integration.{function}()");
        var connection=new NpgsqlConnection(_connectionString);await connection.OpenAsync();var transaction=await connection.BeginTransactionAsync();
        await using var acquire=new NpgsqlCommand("SELECT pg_advisory_xact_lock(@key::bigint)",connection,transaction);acquire.Parameters.AddWithValue("key",key);await acquire.ExecuteNonQueryAsync();
        return new(connection,transaction,trigger,function,key);
    }
    [Fact]
    public async Task Role_last_slot_is_serialized_and_only_one_create_commits()
    {
        var server=(await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        for(var i=0;i<19;i++)(await RoleCreateAsync(server,"Role "+i)).EnsureSuccessStatusCode();
        await using var gate=await AccessGateAsync(server);
        var first=RoleCreateAsync(server,"Last A");var pid=await WaitForGateAsync(gate);
        var second=RoleCreateAsync(server,"Last B");await WaitForBlockedQueryAsync(pid,"%FROM community.servers WHERE id=%FOR UPDATE%");
        await gate.ReleaseAsync();Assert.Equal(HttpStatusCode.Created,(await first).StatusCode);
        await AssertErrorAsync(await second,HttpStatusCode.Conflict,"ROLE_LIMIT_REACHED");
        Assert.Equal(20L,await SqlAsync("SELECT count(*) FROM community.roles WHERE server_id=@id AND NOT is_system",("id",server)));
        Assert.Equal(20L,await AccessEventsAsync(server));
    }
    [Fact]
    public async Task Concurrent_create_same_operation_replays_winner_and_changed_payload_conflicts()
    {
        var server=(await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();var operation=Guid.NewGuid();
        await using var gate=await GateAsync(operation);
        var first=RoleCreateAsync(server,permissions:["manage_channels","manage_invites"],operation:operation);var pid=await WaitForGateAsync(gate);
        var second=RoleCreateAsync(server,permissions:["manage_invites","manage_channels"],operation:operation);
        await WaitForBlockedQueryAsync(pid,"%FROM community.servers WHERE id=%FOR UPDATE%");await gate.ReleaseAsync();
        Assert.Equal(HttpStatusCode.Created,(await first).StatusCode);Assert.Equal(HttpStatusCode.OK,(await second).StatusCode);
        await AssertErrorAsync(await RoleCreateAsync(server,permissions:[],operation:operation),HttpStatusCode.Conflict,"OPERATION_CONFLICT");
        Assert.Equal(1L,await AccessEventsAsync(server));
    }
    [Fact]
    public async Task Concurrent_role_versions_conflict_and_ownership_is_rechecked_after_server_lock()
    {
        var server=(await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        var role=(await JsonAsync(await RoleCreateAsync(server))).GetProperty("id").GetGuid();
        await using(var gate=await AccessGateAsync(server))
        {
            var first=SendAsync(HttpMethod.Patch,$"/api/v1/servers/{server}/roles/{role}",_owner,new{expectedVersion="1",name="First"});var pid=await WaitForGateAsync(gate);
            var second=SendAsync(HttpMethod.Patch,$"/api/v1/servers/{server}/roles/{role}",_owner,new{expectedVersion="1",name="Second"});
            await WaitForBlockedQueryAsync(pid,"%FROM community.servers WHERE id=%FOR UPDATE%");await gate.ReleaseAsync();
            Assert.Equal(HttpStatusCode.OK,(await first).StatusCode);await AssertErrorAsync(await second,HttpStatusCode.Conflict,"VERSION_CONFLICT");
        }
        (await JoinAsync(server)).EnsureSuccessStatusCode();
        await using var connection=new NpgsqlConnection(_connectionString);await connection.OpenAsync();await using var transaction=await connection.BeginTransactionAsync();
        await using var locking=new NpgsqlCommand($"SELECT pg_backend_pid() FROM community.servers WHERE id='{server}' FOR UPDATE",connection,transaction);var blocker=(int)(await locking.ExecuteScalarAsync())!;
        var changing=SendAsync(HttpMethod.Patch,$"/api/v1/servers/{server}/roles/{role}",_owner,new{expectedVersion="2",name="Lost ownership"});
        await WaitForBlockedQueryAsync(blocker,"%FROM community.servers WHERE id=%FOR UPDATE%");
        await using var transfer=new NpgsqlCommand($"UPDATE community.servers SET owner_user_id='{_other.UserId}' WHERE id='{server}'",connection,transaction);await transfer.ExecuteNonQueryAsync();await transaction.CommitAsync();
        await AssertErrorAsync(await changing,HttpStatusCode.Forbidden,"PERMISSION_DENIED");
    }
    [Fact]
    public async Task Outbox_fault_and_expired_lease_roll_back_role_operation_and_versions()
    {
        var server=(await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();var op=Guid.NewGuid();var name="fail_role_"+Guid.NewGuid().ToString("N");
        await SqlAsync($"CREATE FUNCTION integration.{name}() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN IF NEW.event_type='Community.AccessChanged.v1' AND NEW.aggregate_id='{server}' THEN RAISE EXCEPTION 'Injected role fault' USING ERRCODE='23514'; END IF; RETURN NEW; END $$; CREATE TRIGGER {name} BEFORE INSERT ON integration.outbox_events FOR EACH ROW EXECUTE FUNCTION integration.{name}()");
        try{Assert.Equal(HttpStatusCode.InternalServerError,(await RoleCreateAsync(server,operation:op)).StatusCode);}
        finally{await SqlAsync($"DROP TRIGGER {name} ON integration.outbox_events; DROP FUNCTION integration.{name}()");}
        Assert.Equal(0L,await AccessEventsAsync(server));Assert.Equal(1,await SqlAsync("SELECT access_version FROM community.servers WHERE id=@id",("id",server)));
        Assert.Equal(0L,await SqlAsync("SELECT count(*) FROM community.operations WHERE kind='create_role' AND scope_id=@id",("id",server)));
        var clock=new CommunityKeyTests.MutableClock(DateTimeOffset.UtcNow);
        using var factory=_factory.WithWebHostBuilder(b=>b.ConfigureServices(s=>{s.RemoveAll<TimeProvider>();s.AddSingleton<TimeProvider>(clock);}));using var client=factory.CreateClient();
        await using var gate=await AccessGateAsync(server);var creating=RoleCreateAsync(server,operation:op,client:client);await WaitForGateAsync(gate);clock.Advance(TimeSpan.FromDays(31));await gate.ReleaseAsync();
        await AssertErrorAsync(await creating,HttpStatusCode.Unauthorized,"SESSION_INVALID");
        Assert.Equal(0L,await AccessEventsAsync(server));Assert.Equal(1L,await SqlAsync("SELECT count(*) FROM community.roles WHERE server_id=@id",("id",server)));
    }
    [Theory]
    [InlineData("unverified",HttpStatusCode.Forbidden)]
    [InlineData("revoked",HttpStatusCode.Unauthorized)]
    public async Task Role_writes_require_current_identity(string change,HttpStatusCode status)
    {
        var server=(await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        await SqlAsync(change=="unverified"?"UPDATE identity.user_emails SET verified_at=NULL WHERE user_id=@id":"UPDATE identity.auth_sessions SET revoked_at=clock_timestamp() WHERE user_id=@id",("id",_owner.UserId));
        if(change=="revoked")await SqlAsync("UPDATE identity.auth_sessions SET revoked_at=clock_timestamp() WHERE id=@id",("id",_owner.SessionId));
        Assert.Equal(status,(await RoleCreateAsync(server)).StatusCode);Assert.Equal(0L,await AccessEventsAsync(server));
    }
}
