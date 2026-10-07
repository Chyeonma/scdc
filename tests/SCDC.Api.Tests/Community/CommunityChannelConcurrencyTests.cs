using System.Data.Common;
using System.Net;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;
using SCDC.Api.Tests.Infrastructure;
using SCDC.Contracts.Community;
using SCDC.Contracts.Identity;
using SCDC.Contracts.Messaging;
using SCDC.Modules.Community.Infrastructure;

namespace SCDC.Api.Tests.Community;

public sealed partial class CommunityApiTests
{
    [Fact]
    public async Task Channel_create_replay_survives_restart_key_rotation_and_remains_scoped_per_server()
    {
        var server = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        var second = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        var operation = Guid.NewGuid();
        var channel = (await JsonAsync(await ChannelCreateAsync(server, operation: operation))).GetProperty("id").GetGuid();
        await using var restart = new SCDCWebApplicationFactory();
        using var restartedClient = restart.CreateClient();
        var replay = await ChannelCreateAsync(server, operation: operation, client: restartedClient);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(channel, (await JsonAsync(replay)).GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.Created, (await ChannelCreateAsync(second, operation: operation)).StatusCode);
        var original = _factory.Services.GetRequiredService<IOptionsMonitor<CommunityOptions>>().CurrentValue;
        var options = new CommunityOptions
        {
            KeyRingPath = original.KeyRingPath,
            Operations = new OperationKeyOptions
            {
                ActiveKeyId = "next",
                Keys = new(original.Operations.Keys) { ["next"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) }
            }
        };
        using var rotated = _factory.WithWebHostBuilder(b => b.ConfigureServices(s =>
            s.AddSingleton<IOptionsMonitor<CommunityOptions>>(new FixedMonitor(options))));
        using var rotatedClient = rotated.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await ChannelCreateAsync(server, operation: operation, client: rotatedClient)).StatusCode);
        options.Operations.Keys.Remove("test");
        await AssertErrorAsync(await ChannelCreateAsync(server, operation: operation, client: rotatedClient),
            HttpStatusCode.ServiceUnavailable, "FINGERPRINT_KEY_UNAVAILABLE");
        Assert.Equal(1L, await AccessEventsAsync(server));
        Assert.Single(ChannelIds(await JsonAsync(await ChannelListAsync(server))));
    }
    [Fact]
    public async Task Concurrent_channel_create_one_operation_and_concurrent_acl_compare_and_swap_have_one_writer()
    {
        var server = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid(); var op = Guid.NewGuid();
        await using (var gate = await AccessGateAsync(server))
        {
            var first = ChannelCreateAsync(server, operation: op); var pid = await WaitForGateAsync(gate);
            var second = ChannelCreateAsync(server, operation: op); await WaitForBlockedQueryAsync(pid, "%FROM community.servers WHERE id=%FOR UPDATE%");
            await gate.ReleaseAsync(); Assert.Equal(HttpStatusCode.Created, (await first).StatusCode); Assert.Equal(HttpStatusCode.OK, (await second).StatusCode);
        }
        var channel = ChannelIds(await JsonAsync(await ChannelListAsync(server))).Single();
        await using (var gate = await AccessGateAsync(server))
        {
            var first = AclPutAsync(server, channel, "deny"); var pid = await WaitForGateAsync(gate);
            var second = AclPutAsync(server, channel, "allow"); await WaitForBlockedQueryAsync(pid, "%FROM community.servers WHERE id=%FOR UPDATE%");
            await gate.ReleaseAsync(); Assert.Equal(HttpStatusCode.OK, (await first).StatusCode);
            await AssertErrorAsync(await second, HttpStatusCode.Conflict, "VERSION_CONFLICT");
        }
        Assert.Equal(2L, await AccessEventsAsync(server));
    }
    [Theory]
    [InlineData("role")]
    [InlineData("acl")]
    public async Task Channel_admission_guard_holds_server_and_channel_share_locks_until_caller_commit(string mutation)
    {
        var server = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid(); var member = await JsonAsync(await JoinAsync(server));
        var channel = (await JsonAsync(await ChannelCreateAsync(server))).GetProperty("id").GetGuid();
        var role = (await JsonAsync(await RoleCreateAsync(server))).GetProperty("id").GetGuid();
        await using var scope = _factory.Services.CreateAsyncScope();
        var guard = scope.ServiceProvider.GetRequiredService<IChannelAccessGuard>();
        var payload = _other.AccessToken.Split('.')[1].Replace('-', '+').Replace('_', '/');
        using var claims = System.Text.Json.JsonDocument.Parse(Convert.FromBase64String(payload.PadRight((payload.Length + 3) / 4 * 4, '=')));
        var stamp = claims.RootElement.GetProperty("sst").GetGuid();
        await using var connection = new NpgsqlConnection(_connectionString); await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var lease = await guard.AcquireAsync(new(_other.UserId, _other.SessionId, stamp), server, channel, transaction, default);
        Assert.Equal(ChannelAccessStatus.Allowed, lease.Status); Assert.True(lease.CanSendText);
        Assert.Equal(member.GetProperty("membershipId").GetGuid(), lease.MembershipId); Assert.True(lease.IsAllowedAt(DateTimeOffset.UtcNow));
        await using var backend = new NpgsqlCommand("SELECT pg_backend_pid()", connection, transaction); var pid = (int)(await backend.ExecuteScalarAsync())!;
        var changed = mutation == "role" ? RoleSetAsync(server, member, [role]) : AclPutAsync(server, channel, "deny");
        await WaitForBlockedQueryAsync(pid, "%FROM community.servers WHERE id=%FOR UPDATE%");
        Assert.False(changed.IsCompleted); await transaction.CommitAsync(); Assert.Equal(HttpStatusCode.OK, (await changed).StatusCode);
        await using var next = await connection.BeginTransactionAsync();
        var after = await guard.AcquireAsync(new(_other.UserId, _other.SessionId, stamp), server, channel, next, default);
        Assert.Equal(mutation == "acl" ? ChannelAccessStatus.Hidden : ChannelAccessStatus.Allowed, after.Status);
        await next.RollbackAsync();
    }
    [Theory]
    [InlineData("create")]
    [InlineData("acl")]
    public async Task Channel_outbox_fault_rolls_back_space_channel_acl_operation_and_versions(string mutation)
    {
        var server = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid(); var op = Guid.NewGuid();
        Guid channel = Guid.Empty;
        if (mutation == "acl") channel = (await JsonAsync(await ChannelCreateAsync(server))).GetProperty("id").GetGuid();
        var before = await AccessEventsAsync(server); var access = await SqlAsync("SELECT access_version FROM community.servers WHERE id=@server", ("server", server));
        var function = "fail_channel_" + Guid.NewGuid().ToString("N");
        await SqlAsync($"CREATE FUNCTION integration.{function}() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN IF NEW.event_type='Community.AccessChanged.v1' AND NEW.aggregate_id='{server}' THEN RAISE EXCEPTION 'Channel fault' USING ERRCODE='23514'; END IF; RETURN NEW; END $$; CREATE TRIGGER {function} BEFORE INSERT ON integration.outbox_events FOR EACH ROW EXECUTE FUNCTION integration.{function}()");
        try { Assert.Equal(HttpStatusCode.InternalServerError, (await (mutation == "create" ? ChannelCreateAsync(server, operation: op) : AclPutAsync(server, channel, "deny"))).StatusCode); }
        finally { await SqlAsync($"DROP TRIGGER {function} ON integration.outbox_events; DROP FUNCTION integration.{function}()"); }
        Assert.Equal(before, await AccessEventsAsync(server)); Assert.Equal(access, await SqlAsync("SELECT access_version FROM community.servers WHERE id=@server", ("server", server)));
        if (mutation == "create")
        {
            Assert.Equal(0L, await SqlAsync("SELECT count(*) FROM community.operations WHERE scope_id=@server AND kind='create_channel'", ("server", server)));
            Assert.Equal(0L, await SqlAsync("SELECT count(*) FROM messaging.spaces WHERE created_by_user_id=@user AND space_type=3", ("user", _owner.UserId)));
            Assert.Empty(ChannelIds(await JsonAsync(await ChannelListAsync(server))));
        }
        else Assert.Equal("1", (await JsonAsync(await AclAsync(server, channel))).GetProperty("accessVersion").GetString());
    }
    private sealed class FailingChannelLifecycle(IChatSpaceLifecycle inner) : IChatSpaceLifecycle
    {
        public async Task CreateChannelAsync(Guid id, Guid actor, DateTimeOffset now, DbTransaction transaction, CancellationToken ct)
        { await inner.CreateChannelAsync(id, actor, now, transaction, ct); throw new InvalidOperationException("Synthetic lifecycle fault after space insert."); }
    }
    [Fact]
    public async Task Lifecycle_failure_and_expired_commit_lease_do_not_leave_orphaned_spaces()
    {
        var server = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        using var source = _factory.Services.CreateScope(); var original = source.ServiceProvider.GetRequiredService<IChatSpaceLifecycle>();
        using var faulty = _factory.WithWebHostBuilder(b => b.ConfigureServices(s => { s.RemoveAll<IChatSpaceLifecycle>(); s.AddSingleton<IChatSpaceLifecycle>(new FailingChannelLifecycle(original)); }));
        using var faultClient = faulty.CreateClient();
        Assert.Equal(HttpStatusCode.InternalServerError, (await ChannelCreateAsync(server, client: faultClient)).StatusCode);
        var clock = new CommunityKeyTests.MutableClock(DateTimeOffset.UtcNow);
        using var expiring = _factory.WithWebHostBuilder(b => b.ConfigureServices(s => { s.RemoveAll<TimeProvider>(); s.AddSingleton<TimeProvider>(clock); })); using var client = expiring.CreateClient();
        await using (var gate = await AccessGateAsync(server))
        {
            var creating = ChannelCreateAsync(server, client: client); await WaitForGateAsync(gate); clock.Advance(TimeSpan.FromDays(31)); await gate.ReleaseAsync();
            await AssertErrorAsync(await creating, HttpStatusCode.Unauthorized, "SESSION_INVALID");
        }
        Assert.Equal(0L, await SqlAsync("SELECT count(*) FROM messaging.spaces WHERE created_by_user_id=@user AND space_type=3", ("user", _owner.UserId)));
        Assert.Equal(0L, await AccessEventsAsync(server));
    }
    [Fact]
    public async Task Removing_role_override_advances_channel_access_version_and_prevents_stale_acl_replacement()
    {
        var server = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        var channel = (await JsonAsync(await ChannelCreateAsync(server))).GetProperty("id").GetGuid();
        var role = (await JsonAsync(await RoleCreateAsync(server))).GetProperty("id").GetGuid();
        (await AclPutAsync(server, channel, roles: [new { roleId = role, effect = "deny" }])).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(HttpMethod.Delete, $"/api/v1/servers/{server}/roles/{role}?expectedVersion=1", _owner)).StatusCode);
        var after = await JsonAsync(await AclAsync(server, channel)); Assert.Equal("3", after.GetProperty("accessVersion").GetString()); Assert.Empty(after.GetProperty("roleOverrides").EnumerateArray());
        await AssertErrorAsync(await AclPutAsync(server, channel, version: "2"), HttpStatusCode.Conflict, "VERSION_CONFLICT");
    }
}
