using System.Net;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using SCDC.Api.Tests.Infrastructure;
using SCDC.Modules.Community.Infrastructure;

namespace SCDC.Api.Tests.Community;

public sealed partial class CommunityApiTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Role_delete_rolls_back_assignments_overrides_and_versions_on_outbox_fault_then_cascades(int effect)
    {
        var server=(await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        var role=(await JsonAsync(await RoleCreateAsync(server,permissions:["manage_invites"]))).GetProperty("id").GetGuid();
        var member=await JsonAsync(await RoleSetAsync(server,await JsonAsync(await JoinAsync(server)),[role]));
        var channel=Guid.CreateVersion7();
        await SqlAsync("INSERT INTO messaging.spaces(id,space_type) VALUES(@channel,3); INSERT INTO community.channels(space_id,server_id,name,name_key) VALUES(@channel,@server,'fixture-room','fixture-room'); INSERT INTO community.channel_role_overrides(space_id,server_id,role_id,permission_code,effect) VALUES(@channel,@server,@role,'channel_view',@effect)",("channel",channel),("server",server),("role",role),("effect",(short)effect));
        var before=await SqlAsync("SELECT access_version FROM community.servers WHERE id=@server",("server",server));
        var name="test_role_delete_"+Guid.NewGuid().ToString("N");
        await SqlAsync($"CREATE FUNCTION integration.{name}() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN IF NEW.event_type='Community.AccessChanged.v1' AND NEW.payload->>'cause'='role_deleted' AND NEW.aggregate_id='{server}' THEN RAISE EXCEPTION 'Role delete fault' USING ERRCODE='23514'; END IF; RETURN NEW; END $$; CREATE TRIGGER {name} BEFORE INSERT ON integration.outbox_events FOR EACH ROW EXECUTE FUNCTION integration.{name}()");
        try{Assert.Equal(HttpStatusCode.InternalServerError,(await SendAsync(HttpMethod.Delete,$"/api/v1/servers/{server}/roles/{role}?expectedVersion=1",_owner)).StatusCode);}
        finally{await SqlAsync($"DROP TRIGGER {name} ON integration.outbox_events; DROP FUNCTION integration.{name}()");}
        Assert.Equal(before,await SqlAsync("SELECT access_version FROM community.servers WHERE id=@server",("server",server)));
        Assert.Equal("2",(await JsonAsync(await MemberRoleGetAsync(server))).GetProperty("version").GetString());
        Assert.Equal(1L,await SqlAsync("SELECT count(*) FROM community.channel_role_overrides WHERE role_id=@role",("role",role)));
        Assert.Equal(1L,await SqlAsync("SELECT count(*) FROM community.member_roles WHERE role_id=@role",("role",role)));
        Assert.Equal(HttpStatusCode.NoContent,(await SendAsync(HttpMethod.Delete,$"/api/v1/servers/{server}/roles/{role}?expectedVersion=1",_owner)).StatusCode);
        Assert.Equal(0L,await SqlAsync("SELECT count(*) FROM community.channel_role_overrides WHERE role_id=@role",("role",role)));
        Assert.Equal(0L,await SqlAsync("SELECT count(*) FROM community.member_roles WHERE role_id=@role",("role",role)));
        Assert.Equal("3",(await JsonAsync(await MemberRoleGetAsync(server))).GetProperty("version").GetString());
        var stale=await Assert.ThrowsAsync<PostgresException>(()=>SqlAsync("INSERT INTO community.channel_user_overrides(space_id,server_id,user_id,membership_id,permission_code,effect) VALUES(@channel,@server,@user,@epoch,'channel_view',1)",("channel",channel),("server",server),("user",_other.UserId),("epoch",Guid.CreateVersion7())));
        Assert.Equal("23503",stale.SqlState);
    }
    [Fact]
    public async Task Concurrent_member_role_sets_use_one_epoch_and_one_version_winner()
    {
        var server=(await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        var a=(await JsonAsync(await RoleCreateAsync(server,"A"))).GetProperty("id").GetGuid();
        var b=(await JsonAsync(await RoleCreateAsync(server,"B"))).GetProperty("id").GetGuid();
        var member=await JsonAsync(await JoinAsync(server));
        await using var gate=await AccessGateAsync(server);
        var first=RoleSetAsync(server,member,[a]);var pid=await WaitForGateAsync(gate);
        var second=RoleSetAsync(server,member,[b]);await WaitForBlockedQueryAsync(pid,"%FROM community.servers WHERE id=%FOR UPDATE%");
        await gate.ReleaseAsync();Assert.Equal(HttpStatusCode.OK,(await first).StatusCode);
        await AssertErrorAsync(await second,HttpStatusCode.Conflict,"VERSION_CONFLICT");
        var current=await JsonAsync(await MemberRoleGetAsync(server));
        Assert.Equal("2",current.GetProperty("version").GetString());Assert.Equal(a,current.GetProperty("roleIds")[0].GetGuid());
    }
    [Fact]
    public async Task Role_create_operations_survive_restart_rotation_and_are_scoped_per_server()
    {
        var server=(await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        var second=(await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();var op=Guid.NewGuid();
        Assert.Equal(HttpStatusCode.Created,(await RoleCreateAsync(server,operation:op)).StatusCode);
        await using var restart=new SCDCWebApplicationFactory();using var restartedClient=restart.CreateClient();
        Assert.Equal(HttpStatusCode.OK,(await RoleCreateAsync(server,operation:op,client:restartedClient)).StatusCode);
        Assert.Equal(HttpStatusCode.Created,(await RoleCreateAsync(second,operation:op)).StatusCode);
        var original=_factory.Services.GetRequiredService<IOptionsMonitor<CommunityOptions>>().CurrentValue;
        var options=new CommunityOptions{KeyRingPath=original.KeyRingPath,Operations=new OperationKeyOptions{ActiveKeyId="next",Keys=new(original.Operations.Keys){["next"]=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))}}};
        using var rotated=_factory.WithWebHostBuilder(b=>b.ConfigureServices(s=>s.AddSingleton<IOptionsMonitor<CommunityOptions>>(new FixedMonitor(options))));using var rotatedClient=rotated.CreateClient();
        Assert.Equal(HttpStatusCode.OK,(await RoleCreateAsync(server,operation:op,client:rotatedClient)).StatusCode);
        options.Operations.Keys.Remove("test");
        await AssertErrorAsync(await RoleCreateAsync(server,operation:op,client:rotatedClient),HttpStatusCode.ServiceUnavailable,"FINGERPRINT_KEY_UNAVAILABLE");
        Assert.Equal(1L,await AccessEventsAsync(server));
    }
    [Fact]
    public async Task Role_server_lock_timeout_is_an_error_and_no_operation_is_written()
    {
        var server=(await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        await using var connection=new NpgsqlConnection(_connectionString);await connection.OpenAsync();await using var transaction=await connection.BeginTransactionAsync();
        await using var query=new NpgsqlCommand($"SELECT id FROM community.servers WHERE id='{server}' FOR UPDATE",connection,transaction);await query.ExecuteScalarAsync();
        await AssertErrorAsync(await RoleCreateAsync(server),HttpStatusCode.ServiceUnavailable,"COMMUNITY_TEMPORARILY_UNAVAILABLE");
        Assert.Equal(0L,await AccessEventsAsync(server));
        await transaction.RollbackAsync();Assert.Equal(HttpStatusCode.Created,(await RoleCreateAsync(server)).StatusCode);
    }
}
