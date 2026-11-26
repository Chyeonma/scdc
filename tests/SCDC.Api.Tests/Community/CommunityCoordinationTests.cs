using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SCDC.Api.Tests.Identity;
using SCDC.Api.Tests.Infrastructure;
using SCDC.BuildingBlocks.Infrastructure.Authentication;
using SCDC.Contracts.Community;
using SCDC.Contracts.Identity;

namespace SCDC.Api.Tests.Community;

public sealed class CommunityCoordinationTests(RedisAccessDatabase database):IClassFixture<RedisAccessDatabase>,IAsyncLifetime
{
    private SCDCWebApplicationFactory root=null!;
    private WebApplicationFactory<ApiProgram> app=null!;
    private HttpClient client=null!;
    private FaultOwner messaging=null!;
    private JsonElement owner,member;
    private Guid server,membership,channel,otherChannel;
    private string ownerName="",memberName="";
    private BlockingDirectory? directory;
    private async Task<WebApplicationFactory<ApiProgram>> Start(bool blocked=false,bool blockDirectory=false)
    {
        root=new();
        app=root.WithWebHostBuilder(builder=>
        {
            builder.ConfigureAppConfiguration((_,settings)=>settings.AddInMemoryCollection(database.Configuration));
            builder.ConfigureServices(services=>
            {
                services.RemoveAll<ICommunityFenceOwner>();
                services.AddSingleton<ICommunityFenceOwner>(p=>new PostgresCommunityFenceOwner(p.GetRequiredService<IConfiguration>(),AccessOwner.Community));
                services.AddSingleton<ICommunityFenceOwner>(p=>messaging=new FaultOwner(new PostgresCommunityFenceOwner(p.GetRequiredService<IConfiguration>(),AccessOwner.Messaging)){Blocked=blocked});
                if(blockDirectory){services.RemoveAll<IUserDirectory>();directory=new BlockingDirectory();services.AddSingleton<IUserDirectory>(directory);}
            });
        });
        client=app.CreateClient();
        await SCDCWebApplicationFactory.WaitForAccessReadyAsync(client);
        return app;
    }
    public async Task InitializeAsync()
    {
        await Start();ownerName="policy_a_"+Guid.NewGuid().ToString("N")[..12];memberName="policy_b_"+Guid.NewGuid().ToString("N")[..12];
        owner=await Register(ownerName);member=await Register(memberName);
        var created=await Request("/api/v1/servers",owner,new{clientOperationId=Guid.NewGuid(),name="Policy proof",visibility="public"},expected:201);
        server=created.GetProperty("id").GetGuid();membership=created.GetProperty("myMembership").GetProperty("membershipId").GetGuid();
        await Request($"/api/v1/servers/{server}/join",member,new{},expected:200,emptyBody:true);
        channel=(await CreateChannel("affected")).GetProperty("id").GetGuid();otherChannel=(await CreateChannel("unaffected")).GetProperty("id").GetGuid();
    }
    private async Task<JsonElement> Register(string name)
    {
        var registered=await Request("/api/v1/auth/register",null,new{username=name,displayName="Policy test",email=name+"@example.test",password="PolicyPassword123"},expected:201);
        await Request("/api/v1/auth/verify-email",null,new{token=registered.GetProperty("developmentVerificationToken").GetString()},expected:204);
        return await Login(name);
    }
    private Task<JsonElement> Login(string name)=>Request("/api/v1/auth/login",null,new{login=name,password="PolicyPassword123"},expected:200);
    private Task<JsonElement> CreateChannel(string name)=>Request($"/api/v1/servers/{server}/channels",owner,new{clientOperationId=Guid.NewGuid(),name,kind="text"},expected:201,actorMembership:membership);
    private async Task<JsonElement> Request(string path,JsonElement? auth=null,object? body=null,int expected=200,
        string? method=null,Guid? actorMembership=null,bool emptyBody=false)
    {
        using var message=new HttpRequestMessage(new HttpMethod(method??(body is null?"GET":"POST")),path);
        if(auth is JsonElement credentials)message.Headers.Authorization=new AuthenticationHeaderValue("Bearer",credentials.GetProperty("accessToken").GetString());
        if(actorMembership is Guid id)message.Headers.Add("X-SCDC-Membership-Id",id.ToString("D"));
        if(body is not null&&!emptyBody)message.Content=JsonContent.Create(body);
        using var response=await client.SendAsync(message);var text=await response.Content.ReadAsStringAsync();
        Assert.True((int)response.StatusCode==expected,$"{method} {path}: expected {expected}, got {(int)response.StatusCode}: {text}");
        return text.Length==0?default:JsonSerializer.Deserialize<JsonElement>(text);
    }
    private object Deny(string expected="1")=>new{expectedAccessVersion=expected,defaultView="deny",roleOverrides=Array.Empty<object>(),memberOverrides=Array.Empty<object>()};
    private Task<JsonElement> Hide(int expected=200)=>Request($"/api/v1/servers/{server}/channels/{channel}/access",owner,Deny(),expected,"PUT",membership);
    private async Task Wait(Func<Task<bool>> condition)
    {
        var timeout=DateTimeOffset.UtcNow.AddSeconds(15);
        while(!await condition()){if(DateTimeOffset.UtcNow>timeout)throw new TimeoutException("Policy did not converge");await Task.Delay(50);}
    }
    private Task<object?> Sql(string query)=>database.Sql(query);
    [Fact]
    public async Task Pending_policy_keeps_committed_state_blocks_only_affected_scope_and_recovers_after_lost_ack()
    {
        messaging.Blocked=true;
        var pending=await Hide(202);var operation=pending.GetProperty("operationId").GetGuid();
        Assert.Equal("allow",(await Request($"/api/v1/servers/{server}/channels/{channel}/access",owner)).GetProperty("defaultView").GetString());
        await Request($"/api/v1/servers/{server}/channels/{channel}",member,expected:503);
        await Request($"/api/v1/servers/{server}/channels/{otherChannel}",member);
        var duplicate=await Hide(202);Assert.Equal(operation,duplicate.GetProperty("operationId").GetGuid());
        Assert.Equal(1L,await Sql($"SELECT count(*) FROM community.access_changes WHERE server_id='{server}' AND phase=0"));
        await Request($"/api/v1/servers/{server}/access-operations/{operation}",member,expected:404);
        await Request($"/api/v1/servers/{server}/channels/{channel}/access",owner,new{expectedAccessVersion="1",defaultView="allow",roleOverrides=Array.Empty<object>(),memberOverrides=Array.Empty<object>()},503,"PUT",membership);
        messaging.LoseNextReceipt=true;messaging.Blocked=false;
        await Wait(async()=>await Sql($"SELECT phase=1 FROM community.access_changes WHERE operation_id='{operation}'") is true);
        Assert.Equal("committed",(await Request($"/api/v1/servers/{server}/access-operations/{operation}",owner)).GetProperty("phase").GetString());
        await Request($"/api/v1/servers/{server}/channels/{channel}",member,expected:404);
        Assert.Equal(2L,await Sql($"SELECT count(*) FROM community.access_change_acks WHERE operation_id='{operation}'"));
        Assert.Equal(1L,await Sql($"SELECT count(*) FROM community.access_scopes WHERE server_id='{server}' AND channel_id='{channel}' AND current_epoch=2"));
        Assert.Equal(2,await Sql($"SELECT access_version FROM community.channels WHERE space_id='{channel}'"));
    }
    [Fact]
    public async Task Accepted_policy_aborts_if_original_session_is_revoked_even_after_actor_logs_in_again()
    {
        messaging.Blocked=true;var pending=await Hide(202);var operation=pending.GetProperty("operationId").GetGuid();
        await Request("/api/v1/auth/logout",null,new{refreshToken=owner.GetProperty("refreshToken").GetString()},204);
        owner=await Login(ownerName);messaging.Blocked=false;
        await Wait(async()=>await Sql($"SELECT phase=2 FROM community.access_changes WHERE operation_id='{operation}'") is true);
        Assert.Equal("aborted",(await Request($"/api/v1/servers/{server}/access-operations/{operation}",owner)).GetProperty("phase").GetString());
        Assert.Equal("allow",(await Request($"/api/v1/servers/{server}/channels/{channel}/access",owner)).GetProperty("defaultView").GetString());
        await Request($"/api/v1/servers/{server}/channels/{channel}",member);
        Assert.Equal(0L,await Sql($"SELECT count(*) FROM community.access_scopes WHERE pending_operation_id='{operation}'"));
        Assert.Equal(2L,await Sql($"SELECT min_epoch FROM messaging.access_fences WHERE authority=2 AND server_id='{server}' AND channel_id='{channel}' AND user_id='{member.GetProperty("user").GetProperty("id").GetGuid()}'"));
    }
    [Fact]
    public async Task Pending_policy_survives_community_restart_and_commits_once()
    {
        messaging.Blocked=true;var pending=await Hide(202);var operation=pending.GetProperty("operationId").GetGuid();
        client.Dispose();await app.DisposeAsync();await root.DisposeAsync();await Start();
        await Wait(async()=>await Sql($"SELECT phase=1 FROM community.access_changes WHERE operation_id='{operation}'") is true);
        Assert.Equal("committed",(await Request($"/api/v1/servers/{server}/access-operations/{operation}",owner)).GetProperty("phase").GetString());
        Assert.Equal(2,await Sql($"SELECT access_version FROM community.channels WHERE space_id='{channel}'"));
        await Request($"/api/v1/servers/{server}/channels/{channel}",member,expected:404);
    }
    [Fact]
    public async Task Actor_membership_header_is_required_and_cannot_be_replaced_by_target_membership()
    {
        var body=new{clientOperationId=Guid.NewGuid(),name="Blocked",permissions=Array.Empty<string>()};
        await Request($"/api/v1/servers/{server}/roles",owner,body,400);
        var target=(await Request($"/api/v1/servers/{server}/membership/me",member)).GetProperty("membershipId").GetGuid();
        var conflict=await Request($"/api/v1/servers/{server}/roles",owner,body,409,actorMembership:target);
        Assert.Equal("MEMBERSHIP_CHANGED",conflict.GetProperty("errorCode").GetString());
        Assert.Equal(0L,await Sql($"SELECT count(*) FROM community.roles WHERE server_id='{server}' AND NOT is_system"));
    }
    [Fact]
    public async Task Profile_lookup_does_not_retain_community_transaction_or_policy_lock()
    {
        client.Dispose();await app.DisposeAsync();await root.DisposeAsync();await Start(blockDirectory:true);
        var reading=Request($"/api/v1/servers/{server}/members",owner);
        await directory!.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        try
        {
            await Request($"/api/v1/servers/{server}/roles",owner,new{clientOperationId=Guid.NewGuid(),name="Concurrent",permissions=Array.Empty<string>()},201,actorMembership:membership);
        }
        finally{directory.Continue.TrySetResult();}
        Assert.Equal(2,(await reading).GetProperty("items").GetArrayLength());
    }
    [Fact]
    public async Task Management_only_revocation_records_an_empty_but_durable_messaging_receipt()
    {
        var role=await Request($"/api/v1/servers/{server}/roles",owner,new{clientOperationId=Guid.NewGuid(),name="Manager",permissions=new[]{"manage_invites"}},201,actorMembership:membership);
        var memberId=member.GetProperty("user").GetProperty("id").GetGuid();
        var current=await Request($"/api/v1/servers/{server}/members/{memberId}/roles",owner);
        await Request($"/api/v1/servers/{server}/members/{memberId}/roles",owner,new{membershipId=current.GetProperty("membershipId").GetGuid(),expectedVersion=current.GetProperty("version").GetString(),roleIds=new[]{role.GetProperty("id").GetGuid()}},200,"PUT",membership);
        messaging.Blocked=true;
        var pending=await Request($"/api/v1/servers/{server}/roles/{role.GetProperty("id").GetGuid()}",owner,new{expectedVersion="1",permissions=Array.Empty<string>()},202,"PATCH",membership);
        var operation=pending.GetProperty("operationId").GetGuid();
        await Request($"/api/v1/servers/{server}/channels/{channel}",member);
        messaging.Blocked=false;
        await Wait(async()=>await Sql($"SELECT phase=1 FROM community.access_changes WHERE operation_id='{operation}'") is true);
        Assert.Equal(true,await Sql($"SELECT installed_scopes='[]'::jsonb FROM messaging.fence_receipts WHERE authority=2 AND operation_id='{operation}'"));
        Assert.Equal(2L,await Sql($"SELECT current_epoch FROM community.access_scopes WHERE server_id='{server}' AND user_id='{memberId}' AND scope_kind=5 AND permission_code='manage_invites'"));
    }
    [Fact]
    public async Task Deleting_a_deny_role_increases_effective_access_without_waiting_for_a_revocation_owner()
    {
        var role=await Request($"/api/v1/servers/{server}/roles",owner,new{clientOperationId=Guid.NewGuid(),name="Deny",permissions=Array.Empty<string>()},201,actorMembership:membership);
        var roleId=role.GetProperty("id").GetGuid();var memberId=member.GetProperty("user").GetProperty("id").GetGuid();
        var current=await Request($"/api/v1/servers/{server}/members/{memberId}/roles",owner);
        await Request($"/api/v1/servers/{server}/members/{memberId}/roles",owner,new{membershipId=current.GetProperty("membershipId").GetGuid(),expectedVersion=current.GetProperty("version").GetString(),roleIds=new[]{roleId}},200,"PUT",membership);
        await Request($"/api/v1/servers/{server}/channels/{channel}/access",owner,new{expectedAccessVersion="1",defaultView="allow",roleOverrides=new[]{new{roleId,effect="deny"}},memberOverrides=Array.Empty<object>()},200,"PUT",membership);
        await Request($"/api/v1/servers/{server}/channels/{channel}",member,expected:404);
        var count=await Sql($"SELECT count(*) FROM community.access_changes WHERE server_id='{server}'");
        messaging.Blocked=true;
        await Request($"/api/v1/servers/{server}/roles/{roleId}?expectedVersion=1",owner,expected:204,method:"DELETE",actorMembership:membership);
        await Request($"/api/v1/servers/{server}/channels/{channel}",member);
        Assert.Equal(count,await Sql($"SELECT count(*) FROM community.access_changes WHERE server_id='{server}'"));
    }
    [Fact]
    public async Task Renaming_a_role_does_not_advance_authority_epochs_or_contact_a_revocation_owner()
    {
        var role=await Request($"/api/v1/servers/{server}/roles",owner,new{clientOperationId=Guid.NewGuid(),name="Before",permissions=new[]{"manage_invites"}},201,actorMembership:membership);
        messaging.Blocked=true;
        await Request($"/api/v1/servers/{server}/roles/{role.GetProperty("id").GetGuid()}",owner,new{expectedVersion="1",name="After"},200,"PATCH",membership);
        Assert.Equal(0L,await Sql($"SELECT count(*) FROM community.access_changes WHERE server_id='{server}'"));
        Assert.Equal(0L,await Sql($"SELECT count(*) FROM community.access_scopes WHERE server_id='{server}' AND current_epoch>1"));
    }
    [Fact]
    public async Task Community_works_without_any_schema_privilege_on_identity()
    {
        await Sql("REVOKE USAGE ON SCHEMA identity FROM scdc_community");
        try
        {
            Assert.Equal(false,await Sql("SELECT has_schema_privilege('scdc_community','identity','USAGE')"));
            await Request($"/api/v1/servers/{server}",owner);
            await Request($"/api/v1/servers/{server}/members",owner);
            await Request($"/api/v1/servers/{server}/roles",owner,new{clientOperationId=Guid.NewGuid(),name="Independent",permissions=Array.Empty<string>()},201,actorMembership:membership);
            await Hide();
            await Request($"/api/v1/servers/{server}/channels/{channel}",member,expected:404);
        }
        finally { await Sql("GRANT USAGE ON SCHEMA identity TO scdc_community"); }
    }
    [Fact]
    public async Task Community_fence_batches_are_monotone_idempotent_and_reject_conflicting_or_misrouted_commands()
    {
        var settings=new ConfigurationBuilder().AddInMemoryCollection(database.Configuration).Build();
        var handler=new PostgresCommunityFenceOwner(settings,AccessOwner.Messaging);
        var operation=Guid.NewGuid();var batchId=Guid.NewGuid();
        var scope=new CommunityScope(3,Guid.NewGuid(),server,Guid.Empty,Guid.NewGuid(),"",5);
        var batch=new CommunityFenceBatch(operation,batchId,1,new[]{scope});
        await handler.ApplyAsync(batch,default);await handler.ApplyAsync(batch,default);
        var conflict=await Assert.ThrowsAsync<AccessCheckException>(()=>handler.ApplyAsync(batch with {Scopes=new[]{scope with {Epoch=6}}},default));
        Assert.Equal("FENCE_CONFLICT",conflict.Code);
        await handler.ApplyAsync(batch with {BatchId=Guid.NewGuid(),Scopes=new[]{scope with {Epoch=2}}},default);
        Assert.Equal(5L,await Sql($"SELECT min_epoch FROM messaging.access_fences WHERE authority=2 AND user_id='{scope.UserId}' AND server_id='{server}'"));
        var invalid=await Assert.ThrowsAsync<AccessCheckException>(()=>handler.ApplyAsync(batch with {Scopes=new[]{scope with {Kind=5,Permission="server_owner"}}},default));
        Assert.Equal("INVALID_FENCE",invalid.Code);
    }
    [Fact]
    public async Task Identity_permit_issued_before_an_owner_fence_cannot_admit_a_community_transaction()
    {
        var token=owner.GetProperty("accessToken").GetString()!.Split('.')[1].Replace('-','+').Replace('_','/');
        using var claims=JsonDocument.Parse(Convert.FromBase64String(token.PadRight((token.Length+3)/4*4,'=')));
        var values=claims.RootElement;
        var actor=new AccountActor(values.GetProperty("sub").GetGuid(),values.GetProperty("sid").GetGuid(),values.GetProperty("sst").GetGuid(),DateTimeOffset.FromUnixTimeSeconds(values.GetProperty("exp").GetInt64()),membership);
        await using var serviceScope=app.Services.CreateAsyncScope();
        var access=serviceScope.ServiceProvider.GetRequiredService<SCDC.Modules.Community.Infrastructure.Access.CommunityIdentityAccess>();
        var permit=await access.AcquireAsync(actor,IdentityPurpose.CommunityManage,server,Guid.NewGuid(),default);
        var config=new ConfigurationBuilder().AddInMemoryCollection(database.Configuration).Build();
        var handler=new PostgresIdentityFenceOwner(config,AccessOwner.Community);
        await handler.ApplyAsync(new(Guid.NewGuid(),Guid.NewGuid(),1,permit.Scopes.Select(s=>s with {Epoch=s.Epoch+1}).ToArray()),default);
        await using var local=await serviceScope.ServiceProvider.GetRequiredService<SCDC.BuildingBlocks.Infrastructure.Persistence.RelationalWorkScopeFactory>().OpenAsync(default);
        var denied=await Assert.ThrowsAsync<AccessCheckException>(()=>access.AdmitAsync(local,permit,default));
        Assert.Equal("ACCESS_CHANGED",denied.Code);
    }
    private sealed class FaultOwner(ICommunityFenceOwner inner):ICommunityFenceOwner
    {
        public AccessOwner Owner=>inner.Owner;
        public volatile bool Blocked;
        public bool LoseNextReceipt;
        public async Task<CommunityFenceReceipt> ApplyAsync(CommunityFenceBatch batch,CancellationToken ct)
        {
            if(Blocked)throw new AccessCheckException();
            var receipt=await inner.ApplyAsync(batch,ct);
            if(LoseNextReceipt){LoseNextReceipt=false;throw new AccessCheckException();}
            return receipt;
        }
    }
    private sealed class BlockingDirectory:IUserDirectory
    {
        public TaskCompletionSource Started{get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Continue{get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<UserSummary?> FindByIdAsync(Guid id,CancellationToken ct)=>Task.FromResult<UserSummary?>(null);
        public Task<UserSummary?> FindByUsernameAsync(string name,CancellationToken ct)=>Task.FromResult<UserSummary?>(null);
        public async Task<IReadOnlyDictionary<Guid,UserSummary>> FindByIdsAsync(IReadOnlyCollection<Guid> ids,CancellationToken ct)
        {Started.TrySetResult();await Continue.Task.WaitAsync(ct);return new Dictionary<Guid,UserSummary>();}
    }
    public async Task DisposeAsync(){client.Dispose();await app.DisposeAsync();await root.DisposeAsync();}
}
