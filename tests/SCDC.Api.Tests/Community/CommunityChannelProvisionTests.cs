using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SCDC.Contracts.Identity;
using SCDC.Contracts.Messaging;
using SCDC.Contracts.Community;
using SCDC.Modules.Community.Infrastructure;

namespace SCDC.Api.Tests.Community;

public sealed partial class CommunityApiTests
{
    private sealed class ProvisionFaults(IChannelProvisioning inner) : IChannelProvisioning
    {
        public Func<PrepareChannel, CancellationToken, Task<ProvisionStatus>>? Prepare { get; init; }
        public Func<ChannelDecision, CancellationToken, Task<ProvisionStatus>>? Confirm { get; init; }
        public Task<ProvisionStatus> PrepareAsync(PrepareChannel request, CancellationToken ct) => Prepare?.Invoke(request, ct) ?? inner.PrepareAsync(request, ct);
        public Task<ProvisionStatus> ConfirmAsync(ChannelDecision request, CancellationToken ct) => Confirm?.Invoke(request, ct) ?? inner.ConfirmAsync(request, ct);
        public Task<ProvisionStatus> CancelAsync(ChannelDecision request, CancellationToken ct) => inner.CancelAsync(request, ct);
        public Task<ProvisionStatus?> GetAsync(Guid operationId, Guid channelId, CancellationToken ct) => inner.GetAsync(operationId, channelId, ct);
        public Task<ProvisionStatus> ActivateAsync(SCDC.Contracts.Community.ChannelReadyPermit permit, CancellationToken ct) => inner.ActivateAsync(permit, ct);
    }
    private IChannelProvisioning ProvisionOwner => _factory.Services.GetRequiredKeyedService<IChannelProvisioning>(AccessOwner.Community);
    private Task<HttpResponseMessage> ChannelStatusAsync(Guid server, Guid operation, Actor? actor = null)
        => SendAsync(HttpMethod.Get, $"/api/v1/servers/{server}/channel-operations/{operation}", actor ?? _owner);
    private async Task<AccountActor> ChannelActorAsync(Guid server)
    {
        var payload = _owner.AccessToken.Split('.')[1].Replace('-', '+').Replace('_', '/');
        using var claims = System.Text.Json.JsonDocument.Parse(Convert.FromBase64String(payload.PadRight((payload.Length + 3) / 4 * 4, '=')));
        return new(_owner.UserId, _owner.SessionId, claims.RootElement.GetProperty("sst").GetGuid(),
            DateTimeOffset.FromUnixTimeSeconds(claims.RootElement.GetProperty("exp").GetInt64()),
            (Guid)(await SqlAsync("SELECT membership_id FROM community.server_members WHERE server_id=@server AND user_id=@user", ("server", server), ("user", _owner.UserId)))!);
    }

    [Fact]
    public async Task Provision_historical_operation_without_metadata_still_cannot_create_another_resource()
    {
        var server = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid(); var operation = Guid.NewGuid();
        var keys = _factory.Services.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<CommunityOptions>>().CurrentValue.Operations;
        var fingerprint = SCDC.Modules.Community.Infrastructure.Idempotency.OperationFingerprint.ComputeChannel(keys.GetKey(keys.ActiveKeyId),
            _owner.UserId, server, operation, "Gone", null, 1, 1);
        await SqlAsync("INSERT INTO community.operations(actor_user_id,kind,scope_id,client_operation_id,fingerprint_version,key_id,fingerprint,resource_id) VALUES(@actor,'create_channel',@server,@op,1,@key,@hash,@resource)",
            ("actor", _owner.UserId), ("server", server), ("op", operation), ("key", keys.ActiveKeyId), ("hash", fingerprint), ("resource", Guid.CreateVersion7()));
        await AssertErrorAsync(await ChannelCreateAsync(server, "Gone", operation: operation), HttpStatusCode.Conflict, "OPERATION_RESOURCE_REMOVED");
        await AssertErrorAsync(await ChannelCreateAsync(server, "New body", operation: operation), HttpStatusCode.Conflict, "OPERATION_CONFLICT");
        Assert.Empty(ChannelIds(await JsonAsync(await ChannelListAsync(server))));
        Assert.Equal(0L, await SqlAsync("SELECT count(*) FROM community.channel_provisions WHERE client_operation_id=@op", ("op", operation)));
    }

    [Fact]
    public async Task Provision_prepare_commits_with_lost_ack_and_retries_one_space_without_a_shared_transaction()
    {
        var server = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        var operation = Guid.NewGuid(); var calls = 0; var owner = ProvisionOwner;
        var faults = new ProvisionFaults(owner)
        {
            Prepare = async (request, ct) =>
            {
                var result = await owner.PrepareAsync(request, ct);
                if (Interlocked.Increment(ref calls) == 1) throw new TimeoutException("Lost ACK after owner commit.");
                return result;
            }
        };
        using var faultFactory = _factory.WithWebHostBuilder(b => b.ConfigureServices(s =>
        { s.RemoveAllKeyed<IChannelProvisioning>(AccessOwner.Community); s.AddKeyedSingleton<IChannelProvisioning>(AccessOwner.Community, faults); }));
        using var client = faultFactory.CreateClient();
        await SqlAsync("REVOKE USAGE ON SCHEMA messaging FROM scdc_community");
        try
        {
            var response = await ChannelCreateAsync(server, operation: operation, client: client);
            Assert.True(response.StatusCode == HttpStatusCode.Created,
                $"Expected 201, received {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            var channel = (await JsonAsync(response)).GetProperty("id").GetGuid();
            Assert.True(calls >= 2);
            Assert.Equal(1L, await SqlAsync("SELECT count(*) FROM messaging.spaces WHERE id=@id AND provision_state=1", ("id", channel)));
            Assert.Equal(1L, await AccessEventsAsync(server));
            var status = await JsonAsync(await ChannelStatusAsync(server, operation));
            Assert.Equal("completed", status.GetProperty("phase").GetString());
            Assert.Equal(channel, status.GetProperty("resourceId").GetGuid());
            var version = await SqlAsync("SELECT version FROM community.channel_provisions WHERE client_operation_id=@op", ("op", operation));
            (await ChannelStatusAsync(server, operation)).EnsureSuccessStatusCode();
            Assert.Equal(version, await SqlAsync("SELECT version FROM community.channel_provisions WHERE client_operation_id=@op", ("op", operation)));
            Assert.Equal(HttpStatusCode.NotFound, (await ChannelStatusAsync(server, operation, _other)).StatusCode);
        }
        finally { await SqlAsync("GRANT USAGE ON SCHEMA messaging TO scdc_community"); }
    }

    [Fact]
    public async Task Provision_cancel_before_prepare_is_durable_and_conflicting_or_late_commands_cannot_recreate_a_space()
    {
        var owner = ProvisionOwner;
        var request = new PrepareChannel(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), _owner.UserId,
            DateTimeOffset.UtcNow, RandomNumberGenerator.GetBytes(32));
        var decision = new ChannelDecision(request.OperationId, request.ChannelId, request.Fingerprint);
        Assert.Equal(ProvisionPhase.Cancelled, (await owner.CancelAsync(decision, default)).Phase);
        Assert.Equal(ProvisionPhase.Cancelled, (await owner.PrepareAsync(request, default)).Phase);
        Assert.Equal(ProvisionPhase.Cancelled, (await owner.CancelAsync(decision, default)).Phase);
        Assert.Equal(0L, await SqlAsync("SELECT count(*) FROM messaging.spaces WHERE id=@id", ("id", request.ChannelId)));
        await Assert.ThrowsAsync<ProvisionConflictException>(() => owner.ConfirmAsync(decision, default));
        await Assert.ThrowsAsync<ProvisionConflictException>(() => owner.PrepareAsync(request with { Fingerprint = RandomNumberGenerator.GetBytes(32) }, default));
        await Assert.ThrowsAsync<ProvisionConflictException>(() => owner.PrepareAsync(request with { OperationId = Guid.CreateVersion7() }, default));
        await Assert.ThrowsAsync<ProvisionConflictException>(() => owner.PrepareAsync(request with { RoutingGeneration = 2 }, default));
    }

    [Fact]
    public async Task Provision_timeout_cancels_once_and_new_key_reuses_the_name_without_old_cleanup_touching_it()
    {
        var server = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        var clientOperation = Guid.NewGuid(); var owner = ProvisionOwner; PrepareChannel? old = null;
        var faults = new ProvisionFaults(owner)
        {
            Prepare = async (request, ct) => { old = request; await owner.PrepareAsync(request, ct); throw new TimeoutException("Repeatedly lost prepare response."); }
        };
        using var faultFactory = _factory.WithWebHostBuilder(b => b.ConfigureServices(s =>
        { s.RemoveAllKeyed<IChannelProvisioning>(AccessOwner.Community); s.AddKeyedSingleton<IChannelProvisioning>(AccessOwner.Community, faults); }));
        using var client = faultFactory.CreateClient();
        await AssertErrorAsync(await ChannelCreateAsync(server, operation: clientOperation, client: client), HttpStatusCode.Conflict, "CHANNEL_CREATION_CANCELLED");
        Assert.NotNull(old);
        Assert.Empty(ChannelIds(await JsonAsync(await ChannelListAsync(server))));
        var cancelled = await JsonAsync(await ChannelStatusAsync(server, clientOperation));
        Assert.Equal("cancelled", cancelled.GetProperty("phase").GetString());
        Assert.False(cancelled.TryGetProperty("resourceId", out _));
        await AssertErrorAsync(await ChannelCreateAsync(server, operation: clientOperation), HttpStatusCode.Conflict, "CHANNEL_CREATION_CANCELLED");
        await AssertErrorAsync(await ChannelCreateAsync(server, "Other body", operation: clientOperation), HttpStatusCode.Conflict, "OPERATION_CONFLICT");
        var next = await ChannelCreateAsync(server);
        Assert.Equal(HttpStatusCode.Created, next.StatusCode);
        var channel = (await JsonAsync(next)).GetProperty("id").GetGuid();
        Assert.Equal(ProvisionPhase.Cancelled, (await owner.PrepareAsync(old!, default)).Phase);
        await owner.CancelAsync(new(old!.OperationId, old.ChannelId, old.Fingerprint), default);
        Assert.Equal(new[] { channel }, ChannelIds(await JsonAsync(await ChannelListAsync(server))));
        Assert.Equal(1L, await SqlAsync("SELECT count(*) FROM messaging.spaces WHERE id=@id AND provision_state=1", ("id", channel)));
    }

    [Fact]
    public async Task Provision_pending_is_hidden_reserves_name_and_revalidates_the_original_session_before_completion()
    {
        var server = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid(); var operation = Guid.NewGuid();
        var existing = (await JsonAsync(await ChannelCreateAsync(server, "Existing"))).GetProperty("id").GetGuid();
        var owner = ProvisionOwner; var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var faults = new ProvisionFaults(owner)
        {
            Prepare = async (request, ct) => { var result = await owner.PrepareAsync(request, ct); reached.TrySetResult(); await release.Task.WaitAsync(ct); return result; }
        };
        using var faultFactory = _factory.WithWebHostBuilder(b => b.ConfigureServices(s =>
        { s.RemoveAllKeyed<IChannelProvisioning>(AccessOwner.Community); s.AddKeyedSingleton<IChannelProvisioning>(AccessOwner.Community, faults); }));
        using var client = faultFactory.CreateClient();
        var creating = ChannelCreateAsync(server, operation: operation, client: client);
        await reached.Task.WaitAsync(TimeSpan.FromSeconds(10));
        try
        {
            Assert.Equal(new[] { existing }, ChannelIds(await JsonAsync(await ChannelListAsync(server))));
            var status = await JsonAsync(await ChannelStatusAsync(server, operation));
            Assert.Equal("preparing", status.GetProperty("phase").GetString());
            Assert.False(status.TryGetProperty("resourceId", out _));
            await AssertErrorAsync(await ChannelCreateAsync(server), HttpStatusCode.Conflict, "NAME_CONFLICT");
            await AssertErrorAsync(await SendAsync(HttpMethod.Patch, $"/api/v1/servers/{server}/channels/{existing}", _owner,
                new { expectedVersion = "1", name = "Phòng chung" }), HttpStatusCode.Conflict, "NAME_CONFLICT");
            await SqlAsync("UPDATE identity.auth_sessions SET revoked_at=clock_timestamp() WHERE id=@id", ("id", _owner.SessionId));
        }
        finally { release.TrySetResult(); }
        await AssertErrorAsync(await creating, HttpStatusCode.Conflict, "CHANNEL_CREATION_CANCELLED");
        Assert.Equal(1L, await AccessEventsAsync(server));
        Assert.Equal(1L, await SqlAsync("SELECT count(*) FROM messaging.spaces WHERE created_by_user_id=@id AND space_type=3", ("id", _owner.UserId)));
    }

    [Fact]
    public async Task Provision_confirm_loss_recovers_with_a_real_worker_after_restart_and_never_revives_a_deleted_space()
    {
        var server = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid(); var operation = Guid.NewGuid();
        var owner = ProvisionOwner;
        var faults = new ProvisionFaults(owner) { Confirm = (_, _) => throw new TimeoutException("Confirm unavailable.") };
        using (var faultFactory = _factory.WithWebHostBuilder(b => b.ConfigureServices(s =>
        { s.RemoveAllKeyed<IChannelProvisioning>(AccessOwner.Community); s.AddKeyedSingleton<IChannelProvisioning>(AccessOwner.Community, faults); })))
        using (var client = faultFactory.CreateClient())
            Assert.Equal(HttpStatusCode.Created, (await ChannelCreateAsync(server, operation: operation, client: client)).StatusCode);
        var status = await JsonAsync(await ChannelStatusAsync(server, operation));
        Assert.True(status.GetProperty("cleanupPending").GetBoolean());
        var channel = status.GetProperty("resourceId").GetGuid();
        Assert.Equal((short)0, await SqlAsync("SELECT provision_state FROM messaging.spaces WHERE id=@id", ("id", channel)));
        var blocked = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => SqlAsync("INSERT INTO messaging.space_members(space_id,user_id) VALUES(@id,@user)", ("id", channel), ("user", _owner.UserId)));
        Assert.Equal("23514", blocked.SqlState);
        using var restarted = _factory.WithWebHostBuilder(b => b.ConfigureServices(s => s.AddHostedService<ChannelProvisionWorker>()));
        using var restartedClient = restarted.CreateClient();
        using var wait = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (await SqlAsync("SELECT provision_state FROM messaging.spaces WHERE id=@id", ("id", channel)) is short state && state == 0)
            await Task.Delay(25, wait.Token);
        Assert.Equal((short)1, await SqlAsync("SELECT provision_state FROM messaging.spaces WHERE id=@id", ("id", channel)));
        var provision = await owner.GetAsync(status.GetProperty("operationId").GetGuid(), channel, default);
        Assert.Equal(ProvisionPhase.Ready, provision!.Phase);
        var fingerprint = (byte[])(await SqlAsync("SELECT fingerprint FROM messaging.channel_provisions WHERE channel_id=@id", ("id", channel)))!;
        await SqlAsync("UPDATE messaging.spaces SET status=3,deleted_at=clock_timestamp() WHERE id=@id", ("id", channel));
        await Assert.ThrowsAsync<ProvisionConflictException>(() => owner.ConfirmAsync(new(provision.OperationId, channel, fingerprint), default));
    }

    [Fact]
    public async Task Provision_prepared_operation_resumes_after_request_process_stops_without_replacing_ids_or_replaying_events()
    {
        var server = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid(); var operation = Guid.NewGuid();
        var owner = ProvisionOwner;
        // Four permanently failing old decisions fill one worker batch. They must
        // back off rather than starve the newly accepted operation until its deadline.
        using (var blocked = _factory.WithWebHostBuilder(b => b.ConfigureServices(s =>
        { s.RemoveAllKeyed<IChannelProvisioning>(AccessOwner.Community); s.AddKeyedSingleton<IChannelProvisioning>(AccessOwner.Community, new ProvisionFaults(owner) { Confirm = (_, _) => throw new TimeoutException("Lost confirm.") }); })))
        using (var blockedClient = blocked.CreateClient())
        {
            for (var i = 0; i < 4; i++)
            {
                var channel = (await JsonAsync(await ChannelCreateAsync(server, $"Blocked {i}", client: blockedClient))).GetProperty("id").GetGuid();
                await SqlAsync("UPDATE messaging.spaces SET status=3,deleted_at=clock_timestamp() WHERE id=@id; UPDATE community.channel_provisions SET next_attempt_at=clock_timestamp()-interval '1 second',version=version+1 WHERE channel_id=@id", ("id", channel));
            }
        }
        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var faults = new ProvisionFaults(owner)
        {
            Prepare = async (request, ct) => { var result = await owner.PrepareAsync(request, ct); reached.TrySetResult(); await Task.Delay(Timeout.Infinite, ct); return result; }
        };
        var actor = await ChannelActorAsync(server);
        using (var faultFactory = _factory.WithWebHostBuilder(b => b.ConfigureServices(s =>
        { s.RemoveAllKeyed<IChannelProvisioning>(AccessOwner.Community); s.AddKeyedSingleton<IChannelProvisioning>(AccessOwner.Community, faults); })))
        {
            using var client = faultFactory.CreateClient(); using var stop = new CancellationTokenSource();
            var creating = faultFactory.Services.GetRequiredService<ChannelProvisionCoordinator>().CreateAsync(actor, server, new(operation, "Recover", null, "text"), stop.Token);
            await reached.Task.WaitAsync(TimeSpan.FromSeconds(10)); stop.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => creating);
        }
        var before = await JsonAsync(await ChannelStatusAsync(server, operation));
        Assert.Equal("preparing", before.GetProperty("phase").GetString());
        using var restarted = _factory.WithWebHostBuilder(b => b.ConfigureServices(s => s.AddHostedService<ChannelProvisionWorker>()));
        using var restartedClient = restarted.CreateClient(); using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        System.Text.Json.JsonElement after;
        do { await Task.Delay(25, timeout.Token); after = await JsonAsync(await ChannelStatusAsync(server, operation)); }
        while (after.GetProperty("phase").GetString() == "preparing");
        Assert.Equal("completed", after.GetProperty("phase").GetString());
        Assert.Equal(before.GetProperty("operationId").GetGuid(), after.GetProperty("operationId").GetGuid());
        Assert.Equal(5L, await AccessEventsAsync(server));
        Assert.Equal(1L, await SqlAsync("SELECT count(*) FROM messaging.spaces s JOIN messaging.channel_provisions p ON p.channel_id=s.id WHERE p.operation_id=@op", ("op", before.GetProperty("operationId").GetGuid())));
    }

    [Fact]
    public async Task Provision_delayed_confirm_accepts_only_a_current_permit_and_checks_access_and_lifecycle_floors()
    {
        var server = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid(); var operation = Guid.NewGuid();
        var owner = ProvisionOwner;
        using var faultFactory = _factory.WithWebHostBuilder(b => b.ConfigureServices(s =>
        { s.RemoveAllKeyed<IChannelProvisioning>(AccessOwner.Community); s.AddKeyedSingleton<IChannelProvisioning>(AccessOwner.Community, new ProvisionFaults(owner) { Confirm = (_, _) => throw new TimeoutException("Delayed confirm.") }); }));
        using var client = faultFactory.CreateClient();
        var response = await ChannelCreateAsync(server, operation: operation, client: client);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var channel = (await JsonAsync(response)).GetProperty("id").GetGuid();
        await using var scope = _factory.Services.CreateAsyncScope();
        var issuer = scope.ServiceProvider.GetRequiredService<IChannelReadyPermitIssuer>();
        var permit = await issuer.AcquireAsync(await ChannelActorAsync(server), server, channel, default);
        await Assert.ThrowsAsync<ProvisionConflictException>(() => owner.ActivateAsync(permit with { ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1) }, default));
        Assert.Equal((short)0, await SqlAsync("SELECT provision_state FROM messaging.spaces WHERE id=@id", ("id", channel)));
        Assert.Equal(ProvisionPhase.Ready, (await owner.ActivateAsync(permit, default)).Phase);
        var fence = _factory.Services.GetServices<ICommunityFenceOwner>().Single(o => o.Owner == AccessOwner.Messaging);
        var access = permit.Scopes.Single(s => s.Kind == 4);
        await fence.ApplyAsync(new(Guid.CreateVersion7(), Guid.CreateVersion7(), 1, [access with { Epoch = access.Epoch + 1 }]), default);
        await Assert.ThrowsAsync<ProvisionConflictException>(() => owner.ActivateAsync(permit, default));
        var lifecycle = permit.Scopes.Single(s => s.Kind == 6);
        await fence.ApplyAsync(new(Guid.CreateVersion7(), Guid.CreateVersion7(), 1, [lifecycle with { Epoch = 2 }]), default);
        await Assert.ThrowsAsync<ProvisionConflictException>(() => owner.ConfirmAsync(new(permit.OperationId, channel, permit.Fingerprint), default));
        var createdAt = new DateTimeOffset((DateTime)(await SqlAsync("SELECT created_at FROM messaging.channel_provisions WHERE channel_id=@id", ("id", channel)))!);
        await Assert.ThrowsAsync<ProvisionConflictException>(() => owner.PrepareAsync(new(permit.OperationId, channel, server, _owner.UserId,
            createdAt, permit.Fingerprint), default));
    }
}
