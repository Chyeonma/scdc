extern alias CommunityApi;
extern alias IdentityApi;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;
using Google.Protobuf.WellKnownTypes;
using Grpc.AspNetCore.Server;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SCDC.BuildingBlocks.Infrastructure.Transport;
using SCDC.Contracts.Community;
using SCDC.Contracts.Identity;
using SCDC.Contracts.Messaging;
using C = SCDC.Community.Grpc.V1;
using M = SCDC.Messaging.Grpc.V1;
using W = SCDC.Identity.Grpc.V1;
using CommunityProgram = CommunityApi::CommunityProgram;
using IdentityProgram = IdentityApi::IdentityProgram;

namespace SCDC.Api.Tests.Infrastructure;

public sealed class CommunityNetworkTests(CommunityNetworkFixture fixture) : IClassFixture<CommunityNetworkFixture>
{
    [Fact]
    public async Task Community_REST_requires_bearer_header_and_does_not_accept_query_credentials()
    {
        var room = await fixture.Room();
        using var query = await fixture.Community.GetAsync("/api/v1/servers?access_token=" + Uri.EscapeDataString(room.Owner.Token));
        Assert.Equal(HttpStatusCode.Unauthorized, query.StatusCode);
        await fixture.Send(fixture.Community, HttpMethod.Get, "/api/v1/servers", null, room.Owner.Token, 200);
        Assert.DoesNotContain(typeof(ApiProgram).Assembly.GetReferencedAssemblies(), reference =>
            reference.Name is "SCDC.Community.Api" or "SCDC.Community.Application" or "SCDC.Community.Infrastructure" or "SCDC.Community.Domain");
    }

    [Fact]
    public async Task Three_real_hosts_are_ready_with_separate_owner_databases_and_public_routes()
    {
        Assert.Equal(HttpStatusCode.OK, (await fixture.Community.GetAsync("/health/ready")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.Community.GetAsync("/api/v1/servers")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fixture.Messaging.GetAsync("/api/v1/servers")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fixture.Community.GetAsync("/hubs/chat")).StatusCode);
        foreach (var owner in new[] { "Identity", "Community", "Messaging" })
        {
            Assert.Equal(0L, await fixture.Sql(owner, "SELECT count(*) FROM pg_namespace WHERE nspname IN ('identity','community','messaging') AND nspname<>@owner", ("owner", owner.ToLowerInvariant())));
            var host = owner == "Community" ? fixture.CommunityHost.Services : owner == "Messaging" ? fixture.MessagingHost.Services : fixture.IdentityHost.Services;
            var config = host.GetRequiredService<IConfiguration>();
            foreach (var other in new[] { "Identity", "Community", "Messaging" }.Except([owner]))
                Assert.True(string.IsNullOrEmpty(config.GetConnectionString(other)));
        }
    }

    [Fact]
    public async Task Channel_creation_and_replay_keep_one_space_and_a_network_readiness_permit_activates_it()
    {
        var room = await fixture.Room();
        var operation = Guid.NewGuid();
        var command = new { clientOperationId = operation, name = "network-channel", kind = "text" };
        var created = await fixture.Send(fixture.Community, HttpMethod.Post, $"/api/v1/servers/{room.Server}/channels", command, room.Owner.Token, 201, room.Membership);
        var channel = Guid.Parse(created!["id"]!.GetValue<string>());
        await fixture.Send(fixture.Community, HttpMethod.Post, $"/api/v1/servers/{room.Server}/channels", command, room.Owner.Token, 200, room.Membership);
        Assert.Equal(1L, await fixture.Sql("Messaging", "SELECT count(*) FROM messaging.spaces WHERE id=@channel", ("channel", channel)));
        Assert.Equal(1L, await fixture.Sql("Community", "SELECT count(*) FROM community.channel_provisions WHERE client_operation_id=@operation", ("operation", operation)));
        var status = await fixture.Provision.GetAsync(await fixture.Operation(channel), channel, default);
        Assert.Equal(ProvisionPhase.Ready, status!.Phase);
        using var peer = new RpcTransport(fixture.Peer("chat-service"));
        var client = new C.ChannelReadiness.ChannelReadinessClient(peer.Channel(fixture.CommunityRpc, "community-service"));
        var request = new PermitRequest(room.Owner.Session, IdentityPurpose.ChannelRead, channel, Guid.NewGuid());
        var ready = await client.AcquireAsync(new C.ChannelReadyRequest
        {
            Identity = IdentityWire.Write(request, AccessOwner.Messaging), ServerId = room.Server.ToString("D"),
            ChannelId = channel.ToString("D"), MembershipId = room.Membership.ToString("D")
        }, deadline: DateTime.UtcNow.AddSeconds(2));
        Assert.Equal(ProvisionPhase.Ready, (await fixture.Provision.ActivateAsync(CommunityWire.Read(ready), default)).Phase);
        Assert.Equal((short)1, await fixture.Sql("Messaging", "SELECT provision_state FROM messaging.spaces WHERE id=@channel", ("channel", channel)));
        var permit = CommunityWire.Read(ready);
        var lifecycle = permit.Scopes.Single(scope => scope.Kind == 6);
        await fixture.Fence.ApplyAsync(new(Guid.NewGuid(), Guid.NewGuid(), 1, [lifecycle with { Epoch = lifecycle.Epoch + 1 }]), default);
        await Assert.ThrowsAsync<ProvisionConflictException>(() => fixture.Provision.ActivateAsync(permit, default));
    }

    [Fact]
    public async Task Provision_commands_are_idempotent_and_cancelled_operations_cannot_recreate_a_space()
    {
        var operation = Guid.NewGuid(); var channel = Guid.NewGuid();
        var request = new PrepareChannel(operation, channel, Guid.NewGuid(), Guid.NewGuid(),
            DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()), RandomNumberGenerator.GetBytes(32));
        Assert.Equal(ProvisionPhase.Prepared, (await fixture.Provision.PrepareAsync(request, default)).Phase);
        await fixture.Provision.PrepareAsync(request, default);
        await Assert.ThrowsAsync<ProvisionConflictException>(() => fixture.Provision.PrepareAsync(request with { Fingerprint = RandomNumberGenerator.GetBytes(32) }, default));
        var decision = new ChannelDecision(operation, channel, request.Fingerprint);
        await fixture.Provision.ConfirmAsync(decision, default);
        await fixture.Provision.ConfirmAsync(decision, default);
        await Assert.ThrowsAsync<ProvisionConflictException>(() => fixture.Provision.CancelAsync(decision, default));
        Assert.Null(await fixture.Provision.GetAsync(Guid.NewGuid(), Guid.NewGuid(), default));
        var cancelled = request with { OperationId = Guid.NewGuid(), ChannelId = Guid.NewGuid() };
        await fixture.Provision.CancelAsync(new(cancelled.OperationId, cancelled.ChannelId, cancelled.Fingerprint), default);
        Assert.Equal(ProvisionPhase.Cancelled, (await fixture.Provision.PrepareAsync(cancelled, default)).Phase);
        Assert.Equal(0L, await fixture.Sql("Messaging", "SELECT count(*) FROM messaging.spaces WHERE id=@channel", ("channel", cancelled.ChannelId)));
    }

    [Fact]
    public async Task A_lost_prepare_response_after_commit_does_not_duplicate_the_space()
    {
        var request = new PrepareChannel(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()), RandomNumberGenerator.GetBytes(32));
        fixture.Faults.DropPrepare = request.OperationId;
        await Assert.ThrowsAsync<AccessCheckException>(() => fixture.Provision.PrepareAsync(request, default));
        Assert.Equal(ProvisionPhase.Prepared, (await fixture.Provision.PrepareAsync(request, default)).Phase);
        Assert.Equal(1L, await fixture.Sql("Messaging", "SELECT count(*) FROM messaging.spaces WHERE id=@channel", ("channel", request.ChannelId)));
        await fixture.Provision.CancelAsync(new(request.OperationId, request.ChannelId, request.Fingerprint), default);
        Assert.Equal(0L, await fixture.Sql("Messaging", "SELECT count(*) FROM messaging.spaces WHERE id=@channel", ("channel", request.ChannelId)));
    }

    [Fact]
    public async Task Lost_confirm_ack_remains_pending_until_a_restarted_Community_worker_reconciles_it()
    {
        var room = await fixture.Room();
        fixture.Faults.LoseNextConfirm = true;
        try
        {
            var channel = await fixture.Channel(room);
            Assert.Equal(true, await fixture.Sql("Community", "SELECT delivery_pending FROM community.channel_provisions WHERE channel_id=@channel", ("channel", channel)));
            await fixture.RestartCommunity();
            fixture.Faults.BlockConfirm = false;
            await fixture.Until(async () => await fixture.Sql("Community", "SELECT delivery_pending FROM community.channel_provisions WHERE channel_id=@channel", ("channel", channel)) is false);
            Assert.Equal(1L, await fixture.Sql("Messaging", "SELECT count(*) FROM messaging.spaces WHERE id=@channel", ("channel", channel)));
            Assert.Equal(ProvisionPhase.Ready, (await fixture.Provision.GetAsync(await fixture.Operation(channel), channel, default))!.Phase);
        }
        finally { fixture.Faults.LoseNextConfirm = false; fixture.Faults.BlockConfirm = false; }
    }

    [Fact]
    public async Task Remote_deadline_and_caller_cancellation_stop_before_owner_work_and_do_not_retry()
    {
        var request = new PrepareChannel(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, RandomNumberGenerator.GetBytes(32));
        fixture.Faults.DelayPrepare = request.OperationId;
        var clock = Stopwatch.StartNew();
        await Assert.ThrowsAsync<AccessCheckException>(() => fixture.Provision.PrepareAsync(request, default));
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(3.5));
        Assert.Equal(1, fixture.Faults.Delayed);
        Assert.Null(await fixture.Provision.GetAsync(request.OperationId, request.ChannelId, default));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        clock.Restart();
        var error = await Record.ExceptionAsync(() => fixture.Provision.PrepareAsync(request, cancellation.Token));
        Assert.True(error is OperationCanceledException or AccessCheckException);
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(1));
        Assert.Equal(2, fixture.Faults.Delayed);
        fixture.Faults.DelayPrepare = Guid.Empty;
    }

    [Fact]
    public async Task A_mismatched_receipt_is_rejected_and_the_committed_fence_can_be_replayed()
    {
        var batch = new CommunityFenceBatch(Guid.NewGuid(), Guid.NewGuid(), 1,
            [new(6, Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, "", 4)]);
        fixture.Faults.CorruptFence = batch.OperationId;
        await Assert.ThrowsAsync<AccessCheckException>(() => fixture.Fence.ApplyAsync(batch, default));
        var receipt = await fixture.Fence.ApplyAsync(batch, default);
        Assert.Equal(batch.BatchId, receipt.Batch.BatchId);
        Assert.Equal(1L, await fixture.Sql("Messaging", "SELECT count(*) FROM messaging.fence_receipts WHERE operation_id=@operation", ("operation", batch.OperationId)));
        await Assert.ThrowsAsync<AccessCheckException>(() => fixture.Fence.ApplyAsync(batch with { Scopes = [batch.Scopes[0] with { Epoch = 5 }] }, default));
    }

    [Fact]
    public async Task Community_cannot_forge_Identity_fences_or_request_owner_wide_Messaging_permits()
    {
        using var peer = new RpcTransport(fixture.Peer("community-service"));
        var owner = new M.MessagingFence.MessagingFenceClient(peer.Channel(fixture.MessagingRpc, "chat-service"));
        var error = await Assert.ThrowsAsync<RpcException>(async () => await owner.ApplyAsync(new W.FenceBatch(), deadline: DateTime.UtcNow.AddSeconds(2)));
        Assert.Equal(StatusCode.PermissionDenied, error.StatusCode);
        var identity = new W.IdentityAccess.IdentityAccessClient(peer.Channel(fixture.IdentityRpc, "identity-service"));
        error = await Assert.ThrowsAsync<RpcException>(async () => await identity.AcquirePermitAsync(new W.AccessRequest
        { Owner = W.Owner.Messaging, Purpose = W.Purpose.DmSendNew }, deadline: DateTime.UtcNow.AddSeconds(2)));
        Assert.Equal(StatusCode.PermissionDenied, error.StatusCode);
        using var wrong = new RpcTransport(fixture.Peer("identity-service"));
        var provisioning = new M.ChannelProvisioning.ChannelProvisioningClient(wrong.Channel(fixture.MessagingRpc, "chat-service"));
        error = await Assert.ThrowsAsync<RpcException>(async () => await provisioning.PrepareAsync(new M.PrepareChannel(), deadline: DateTime.UtcNow.AddSeconds(2)));
        Assert.Equal(StatusCode.PermissionDenied, error.StatusCode);
    }

    [Fact]
    public async Task Invalid_wire_data_is_rejected_before_creating_owner_state()
    {
        using var peer = new RpcTransport(fixture.Peer("community-service"));
        var client = new M.ChannelProvisioning.ChannelProvisioningClient(peer.Channel(fixture.MessagingRpc, "chat-service"));
        var error = await Assert.ThrowsAsync<RpcException>(async () => await client.PrepareAsync(new M.PrepareChannel(), deadline: DateTime.UtcNow.AddSeconds(2)));
        Assert.Equal(StatusCode.InvalidArgument, error.StatusCode);
        var fences = new M.CommunityPolicyFence.CommunityPolicyFenceClient(peer.Channel(fixture.MessagingRpc, "chat-service"));
        error = await Assert.ThrowsAsync<RpcException>(async () => await fences.ApplyAsync(new C.CommunityFenceBatch(), deadline: DateTime.UtcNow.AddSeconds(2)));
        Assert.Equal(StatusCode.InvalidArgument, error.StatusCode);
    }

    [Fact]
    public async Task Policy_revocation_finishes_after_a_lost_Messaging_ack_and_denies_the_old_access()
    {
        var room = await fixture.Room(); var member = await fixture.Actor();
        await fixture.Send(fixture.Community, HttpMethod.Post, $"/api/v1/servers/{room.Server}/join", null, member.Token, 200);
        var channel = await fixture.Channel(room);
        fixture.Faults.DropNextFence = true;
        await fixture.Send(fixture.Community, HttpMethod.Put, $"/api/v1/servers/{room.Server}/channels/{channel}/access",
            new { expectedAccessVersion = "1", defaultView = "deny", roleOverrides = Array.Empty<object>(), memberOverrides = Array.Empty<object>() },
            room.Owner.Token, [200, 202], room.Membership);
        await fixture.Until(async () => (long)(await fixture.Sql("Community", "SELECT count(*) FROM community.access_changes WHERE server_id=@server AND phase=0", ("server", room.Server)))! == 0);
        await fixture.Send(fixture.Community, HttpMethod.Get, $"/api/v1/servers/{room.Server}/channels/{channel}", null, member.Token, 404);
        Assert.True(fixture.Faults.DroppedFences > 0);
        Assert.Equal(2L, await fixture.Sql("Community", "SELECT count(*) FROM community.access_change_acks WHERE operation_id=(SELECT operation_id FROM community.access_changes WHERE server_id=@server ORDER BY created_at DESC LIMIT 1)", ("server", room.Server)));
    }

    [Fact]
    public async Task Pending_policy_survives_Community_and_Messaging_restart_without_blocking_independent_reads()
    {
        var room = await fixture.Room(); var member = await fixture.Actor();
        await fixture.Send(fixture.Community, HttpMethod.Post, $"/api/v1/servers/{room.Server}/join", null, member.Token, 200);
        var channel = await fixture.Channel(room);
        await fixture.StopMessaging();
        using var unavailable = await fixture.Community.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailable.StatusCode);
        Assert.Equal("ACCESS_CHECK_UNAVAILABLE", (await unavailable.Content.ReadFromJsonAsync<JsonNode>())!["code"]!.GetValue<string>());
        await fixture.Send(fixture.Community, HttpMethod.Get, $"/api/v1/servers/{room.Server}", null, room.Owner.Token, 200);
        var cancelledCreate = Guid.NewGuid();
        await fixture.Send(fixture.Community, HttpMethod.Post, $"/api/v1/servers/{room.Server}/channels",
            new { clientOperationId = cancelledCreate, name = "while-messaging-is-down", kind = "text" }, room.Owner.Token, [409, 503], room.Membership);
        var pending = await fixture.Send(fixture.Community, HttpMethod.Put, $"/api/v1/servers/{room.Server}/channels/{channel}/access",
            new { expectedAccessVersion = "1", defaultView = "deny", roleOverrides = Array.Empty<object>(), memberOverrides = Array.Empty<object>() },
            room.Owner.Token, 202, room.Membership);
        var operation = Guid.Parse(pending!["operationId"]!.GetValue<string>());
        Assert.Equal((short)0, await fixture.Sql("Community", "SELECT phase FROM community.access_changes WHERE operation_id=@operation", ("operation", operation)));
        await fixture.RestartCommunity();
        await fixture.StartMessaging();
        await fixture.Until(async () => (short)(await fixture.Sql("Community", "SELECT phase FROM community.access_changes WHERE operation_id=@operation", ("operation", operation)))! == 1);
        await fixture.Ready();
        await fixture.Send(fixture.Community, HttpMethod.Get, $"/api/v1/servers/{room.Server}/channels/{channel}", null, member.Token, 404);
        // Terminal decisions retain the durable backoff (up to 30 seconds) across restart.
        await fixture.Until(async () => await fixture.Sql("Community", "SELECT delivery_pending FROM community.channel_provisions WHERE client_operation_id=@operation", ("operation", cancelledCreate)) is false, TimeSpan.FromSeconds(45));
        var cancelledChannel = (Guid)(await fixture.Sql("Community", "SELECT channel_id FROM community.channel_provisions WHERE client_operation_id=@operation", ("operation", cancelledCreate)))!;
        Assert.Equal(0L, await fixture.Sql("Messaging", "SELECT count(*) FROM messaging.spaces WHERE id=@channel", ("channel", cancelledChannel)));
    }

    [Fact]
    public async Task Logout_reaches_both_remote_owners_and_closes_a_real_Messaging_connection()
    {
        var actor = await fixture.Actor();
        await using var hub = new HubConnectionBuilder().WithUrl(fixture.Messaging.BaseAddress + "hubs/chat",
            options => options.AccessTokenProvider = () => Task.FromResult<string?>(actor.Token)).Build();
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        hub.Closed += _ => { closed.TrySetResult(); return Task.CompletedTask; };
        await hub.StartAsync();
        var clock = Stopwatch.StartNew();
        await fixture.Send(fixture.Identity, HttpMethod.Post, "/api/v1/auth/logout", new { refreshToken = actor.Refresh }, null, [204, 202]);
        await closed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(5));
        await fixture.Until(async () => (long)(await fixture.Sql("Identity", "SELECT count(*) FROM identity.access_changes WHERE user_id=@user AND phase=0", ("user", actor.Session.UserId)))! == 0);
        foreach (var owner in new[] { "Community", "Messaging" })
            Assert.True((long)(await fixture.Sql(owner, $"SELECT count(*) FROM {owner.ToLowerInvariant()}.fence_receipts WHERE authority=1"))! > 0);
        await fixture.Send(fixture.Community, HttpMethod.Get, "/api/v1/servers", null, actor.Token, 401);
    }
}

public sealed class CommunityRpcFaults : Interceptor
{
    public Guid DropPrepare, CorruptFence, DelayPrepare;
    public bool DropNextFence, LoseNextConfirm, BlockConfirm;
    public int DroppedFences, Delayed;
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        if (request is M.PrepareChannel delayed && delayed.OperationId == DelayPrepare.ToString("D"))
        { Interlocked.Increment(ref Delayed); await Task.Delay(TimeSpan.FromSeconds(10), context.CancellationToken); }
        if (BlockConfirm && context.Method.EndsWith("/Confirm", StringComparison.Ordinal))
            throw new RpcException(new Status(StatusCode.Unavailable, "Injected interrupted confirm transport."));
        var response = await continuation(request, context);
        if (LoseNextConfirm && context.Method.EndsWith("/Confirm", StringComparison.Ordinal))
        {
            LoseNextConfirm = false; BlockConfirm = true;
            throw new RpcException(new Status(StatusCode.Unavailable, "Injected lost confirm response."));
        }
        if (request is M.PrepareChannel prepare && prepare.OperationId == DropPrepare.ToString("D"))
        { DropPrepare = Guid.Empty; throw new RpcException(new Status(StatusCode.Unavailable, "Injected lost prepare response.")); }
        if (request is C.CommunityFenceBatch batch)
        {
            if (DropNextFence) { DropNextFence = false; DroppedFences++; throw new RpcException(new Status(StatusCode.Unavailable, "Injected lost fence response.")); }
            if (batch.OperationId == CorruptFence.ToString("D") && response is C.CommunityFenceReceipt receipt)
            { CorruptFence = Guid.Empty; receipt.Batch.BatchId = Guid.NewGuid().ToString("D"); }
        }
        return response;
    }
}

public sealed class CommunityNetworkFixture : IAsyncLifetime
{
    public WebApplicationFactory<CommunityProgram> CommunityHost { get; private set; } = null!;
    public WebApplicationFactory<ApiProgram> MessagingHost { get; private set; } = null!;
    public WebApplicationFactory<IdentityProgram> IdentityHost { get; private set; } = null!;
    public HttpClient Community { get; private set; } = null!;
    public HttpClient Messaging { get; private set; } = null!;
    public HttpClient Identity { get; private set; } = null!;
    public string CommunityRpc { get; private set; } = "";
    public string MessagingRpc { get; private set; } = "https://127.0.0.1:1/";
    public string IdentityRpc { get; private set; } = "";
    public CommunityRpcFaults Faults { get; } = new();
    public IChannelProvisioning Provision => CommunityHost.Services.GetRequiredKeyedService<IChannelProvisioning>(AccessOwner.Community);
    public ICommunityFenceOwner Fence => CommunityHost.Services.GetServices<ICommunityFenceOwner>().Single(owner => owner.Owner == AccessOwner.Messaging);
    private readonly string directory = Directory.CreateTempSubdirectory("scdc-community-rpc-").FullName;
    private readonly string suffix = Guid.NewGuid().ToString("N");
    private readonly string operationKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private readonly Dictionary<string, string> databases = [];
    private string admin = "";
    private bool messagingRunning;
    private static string Root => SCDC.Api.Tests.Community.CommunityMigrationTests.RepoRoot;
    public sealed record ActorInfo(string Token, string Refresh, AuthenticatedSession Session);
    public sealed record RoomInfo(ActorInfo Owner, Guid Server, Guid Membership);

    public IConfiguration Peer(string peer) => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["InternalRpc:CaPath"] = Path.Combine(directory, "ca.crt"),
        ["InternalRpc:CertificatePath"] = Path.Combine(directory, peer + ".crt"),
        ["InternalRpc:PrivateKeyPath"] = Path.Combine(directory, peer + ".key")
    }).Build();

    public async Task InitializeAsync()
    {
        Certificates();
        admin = Environment.GetEnvironmentVariable("ConnectionStrings__TestDatabase") ?? throw new InvalidOperationException("Test database is required.");
        if (!new NpgsqlConnectionStringBuilder(admin).Database!.EndsWith("_test", StringComparison.Ordinal)) throw new InvalidOperationException("An isolated test database is required.");
        foreach (var owner in new[] { "Identity", "Community", "Messaging" })
        {
            var name = "scdc_c_rpc_" + owner.ToLowerInvariant() + "_" + suffix + "_test";
            await Execute(admin, "CREATE DATABASE " + name);
            databases[owner] = new NpgsqlConnectionStringBuilder(admin) { Database = name, Pooling = false }.ConnectionString;
            if (owner == "Community")
            {
                foreach (var file in new[] { "schema.sql", "baseline.sql", "runtime-grants.sql" })
                    await Execute(databases[owner], await File.ReadAllTextAsync(Path.Combine(Root, "database/postgres/community", file)));
            }
            else if (owner == "Identity")
            {
                await Execute(databases[owner], "CREATE EXTENSION pg_trgm;");
                await Execute(databases[owner], await File.ReadAllTextAsync(Path.Combine(Root, "database/postgres/identity/schema.sql")));
                var baseline = await File.ReadAllTextAsync(Path.Combine(Root, "database/postgres/identity/baseline.sql"));
                baseline = baseline.Replace(":'baseline_checksum'", "'" + new string('0', 64) + "'", StringComparison.Ordinal);
                await Execute(databases[owner], baseline);
                await Execute(databases[owner], await File.ReadAllTextAsync(Path.Combine(Root, "database/postgres/identity/runtime-grants.sql")));
                await IdentityEmailTestDatabase.UpgradeAsync(databases[owner]);
            }
            else
            {
                var schema = await File.ReadAllTextAsync(Path.Combine(Root, "database/postgres/schema.sql"));
                await Execute(databases[owner], string.Join('\n', schema.Split('\n').Where(line => !line.TrimStart().StartsWith('\\'))));
                var grants = await File.ReadAllTextAsync(Path.Combine(Root, "database/postgres/module-roles.sql"));
                await Execute(databases[owner], "BEGIN;\n" + grants[grants.IndexOf("REVOKE CREATE ON SCHEMA", StringComparison.Ordinal)..]);
                await Execute(databases[owner], "DROP SCHEMA identity CASCADE; DROP SCHEMA community CASCADE;");
            }
        }
        IdentityHost = new WebApplicationFactory<IdentityProgram>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(Settings("identity-service", "Identity", ["community-service", "chat-service"])));
        });
        IdentityHost.UseKestrel(0); IdentityHost.StartServer();
        IdentityRpc = Address(IdentityHost.Services, "https");
        Identity = new HttpClient { BaseAddress = new Uri(Address(IdentityHost.Services, "http")) };
        await StartMessaging();
        StartCommunity();
        await Ready();
    }

    private Dictionary<string, string?> Settings(string peer, string owner, string[] allowed)
    {
        var values = TestJwtKeys.Configuration();
        foreach (var item in Peer(peer).AsEnumerable()) values[item.Key] = item.Value;
        foreach (var other in new[] { "Identity", "Community", "Messaging" }) values["ConnectionStrings:" + other] = "";
        var runtime = Environment.GetEnvironmentVariable("ConnectionStrings__" + owner) ?? throw new InvalidOperationException("Runtime test role is required.");
        values["ConnectionStrings:" + owner] = new NpgsqlConnectionStringBuilder(runtime)
        { Database = new NpgsqlConnectionStringBuilder(databases[owner]).Database, Pooling = false }.ConnectionString;
        for (var index = 0; index < allowed.Length; index++) values[$"InternalRpc:AllowedPeers:{index}"] = allowed[index];
        values["InternalRpc:Port"] = "0"; values["InternalRpc:HttpPort"] = "0";
        values["InternalRpc:Identity:Endpoint"] = IdentityRpc;
        values["InternalRpc:Messaging:Endpoint"] = MessagingRpc;
        values["InternalRpc:Community:Endpoint"] = CommunityRpc.Length == 0 ? "https://127.0.0.1:1/" : CommunityRpc;
        values["InternalRpc:Community:Peer"] = "community-service";
        values["InternalRpc:PeerOwners:community-service:0"] = "Community";
        values["InternalRpc:PeerOwners:chat-service:0"] = "Messaging";
        values["InternalRpc:PermitDelegations:community-service:Messaging:0"] = "ChannelRead";
        values["Authentication:Sessions:KeyPrefix"] = "scdc:auth:community-rpc:" + suffix + ":";
        values["Modules:Identity:ExposeDevelopmentTokens"] = "true";
        values["Modules:Identity:Email:KeyRingPath"] = Path.Combine(directory, "identity-email-keys");
        if (owner != "Identity") values["Modules:Identity:PrivateKeyPath"] = "";
        values["Modules:Identity:UserSearch:CursorKeyRingPath"] = Path.Combine(directory, "identity-cursors");
        values["Modules:Messaging:Dispatcher:Enabled"] = "true";
        values["Modules:Community:Operations:ActiveKeyId"] = "network";
        values["Modules:Community:Operations:Keys:network"] = operationKey;
        values["Modules:Community:KeyRingPath"] = Path.Combine(directory, "community-keys");
        values["Hosting:CommunityEnabled"] = "false";
        values["CommunityHost:Integrated"] = "true";
        values["Telemetry:Enabled"] = "false";
        return values;
    }

    public Task StartMessaging()
    {
        MessagingHost = new WebApplicationFactory<ApiProgram>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Hosting:CommunityEnabled", "false");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(Settings("chat-service", "Messaging", ["identity-service", "community-service"])));
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(Faults);
                services.Configure<GrpcServiceOptions>(options => options.Interceptors.Add<CommunityRpcFaults>());
            });
        });
        MessagingHost.UseKestrel(0); MessagingHost.StartServer();
        MessagingRpc = Address(MessagingHost.Services, "https");
        Messaging = new HttpClient { BaseAddress = new Uri(Address(MessagingHost.Services, "http")) };
        messagingRunning = true;
        IdentityHost.Services.GetRequiredService<IConfiguration>()["InternalRpc:Messaging:Endpoint"] = MessagingRpc;
        if (CommunityHost is not null) CommunityHost.Services.GetRequiredService<IConfiguration>()["InternalRpc:Messaging:Endpoint"] = MessagingRpc;
        return Task.CompletedTask;
    }
    public async Task StopMessaging()
    {
        Messaging.Dispose(); await MessagingHost.DisposeAsync(); messagingRunning = false;
    }
    private void StartCommunity()
    {
        CommunityHost = new WebApplicationFactory<CommunityProgram>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("CommunityHost:Integrated", "true");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(Settings("community-service", "Community", ["identity-service", "chat-service"])));
        });
        CommunityHost.UseKestrel(0); CommunityHost.StartServer();
        CommunityRpc = Address(CommunityHost.Services, "https");
        Community = new HttpClient { BaseAddress = new Uri(Address(CommunityHost.Services, "http")) };
        IdentityHost.Services.GetRequiredService<IConfiguration>()["InternalRpc:Community:Endpoint"] = CommunityRpc;
    }
    public async Task RestartCommunity()
    {
        Community.Dispose(); await CommunityHost.DisposeAsync(); StartCommunity();
    }
    public async Task Ready()
    {
        await SCDCWebApplicationFactory.WaitForAccessReadyAsync(Identity, "/api/v1/health/identity/access");
        await SCDCWebApplicationFactory.WaitForAccessReadyAsync(Messaging);
        await SCDCWebApplicationFactory.WaitForAccessReadyAsync(Community, "/health/ready");
    }
    public async Task<ActorInfo> Actor()
    {
        var name = "c_rpc_" + Guid.NewGuid().ToString("N")[..16];
        var created = await Send(Identity, HttpMethod.Post, "/api/v1/auth/register", new { username = name, displayName = name, email = name + "@example.test", password = "CommunityNetwork!2026" }, null, 201);
        await Send(Identity, HttpMethod.Post, "/api/v1/auth/verify-email", new { token = created!["developmentVerificationToken"]!.GetValue<string>() }, null, 204);
        var login = await Send(Identity, HttpMethod.Post, "/api/v1/auth/login", new { login = name, password = "CommunityNetwork!2026" }, null, 200);
        var token = login!["accessToken"]!.GetValue<string>();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        string Claim(string name) => jwt.Claims.Single(claim => claim.Type == name).Value;
        return new(token, login["refreshToken"]!.GetValue<string>(), new(Guid.Parse(Claim("sub")), Guid.Parse(Claim("sid")), Guid.Parse(Claim("sst")), DateTimeOffset.FromUnixTimeSeconds(long.Parse(Claim("exp")))));
    }
    public async Task<RoomInfo> Room()
    {
        var actor = await Actor();
        var result = await Send(Community, HttpMethod.Post, "/api/v1/servers", new { clientOperationId = Guid.NewGuid(), name = "RPC server", visibility = "public" }, actor.Token, 201);
        return new(actor, Guid.Parse(result!["id"]!.GetValue<string>()), Guid.Parse(result["myMembership"]!["membershipId"]!.GetValue<string>()));
    }
    public async Task<Guid> Channel(RoomInfo room)
    {
        var result = await Send(Community, HttpMethod.Post, $"/api/v1/servers/{room.Server}/channels", new { clientOperationId = Guid.NewGuid(), name = "channel", kind = "text" }, room.Owner.Token, 201, room.Membership);
        return Guid.Parse(result!["id"]!.GetValue<string>());
    }
    public Task<JsonNode?> Send(HttpClient client, HttpMethod method, string path, object? body, string? token, int expected, Guid? member = null) =>
        Send(client, method, path, body, token, [expected], member);
    public async Task<JsonNode?> Send(HttpClient client, HttpMethod method, string path, object? body, string? token, int[] expected, Guid? member = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null) request.Content = JsonContent.Create(body);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (member is not null) request.Headers.Add("X-SCDC-Membership-Id", member.Value.ToString("D"));
        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        var json = text.Length == 0 ? null : JsonNode.Parse(text);
        Assert.True(expected.Contains((int)response.StatusCode), $"{method} {path}: expected {string.Join(',', expected)}, got {(int)response.StatusCode}, code {json?["code"]}");
        return json;
    }
    public async Task<Guid> Operation(Guid channel) => (Guid)(await Sql("Community", "SELECT operation_id FROM community.channel_provisions WHERE channel_id=@channel", ("channel", channel)))!;
    public async Task<object?> Sql(string owner, string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(databases[owner]); await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        return await command.ExecuteScalarAsync();
    }
    public async Task Until(Func<Task<bool>> predicate, TimeSpan? timeout = null)
    {
        var clock = Stopwatch.StartNew();
        while (!await predicate()) { Assert.True(clock.Elapsed < (timeout ?? TimeSpan.FromSeconds(20)), "Persisted work did not finish."); await Task.Delay(100); }
    }
    private static async Task Execute(string settings, string sql)
    {
        await using var connection = new NpgsqlConnection(settings); await connection.OpenAsync();
        await new NpgsqlCommand(sql, connection) { CommandTimeout = 30 }.ExecuteNonQueryAsync();
    }
    private static string Address(IServiceProvider services, string scheme) => services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses
        .First(value => value.StartsWith(scheme + ":", StringComparison.Ordinal)).Replace("[::]", "127.0.0.1").TrimEnd('/') + "/";
    private void Certificates()
    {
        using var rootKey = RSA.Create(2048);
        var root = new CertificateRequest("CN=Community network tests", rootKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        root.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        root.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true));
        using var ca = root.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(2));
        File.WriteAllText(Path.Combine(directory, "ca.crt"), ca.ExportCertificatePem());
        foreach (var peer in new[] { "identity-service", "chat-service", "community-service" })
        {
            using var key = RSA.Create(2048);
            var request = new CertificateRequest("CN=" + peer, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var san = new SubjectAlternativeNameBuilder(); san.AddDnsName(peer); san.AddIpAddress(IPAddress.Loopback);
            request.CertificateExtensions.Add(san.Build());
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new("1.3.6.1.5.5.7.3.1"), new("1.3.6.1.5.5.7.3.2") }, true));
            using var certificate = request.Create(ca, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1), RandomNumberGenerator.GetBytes(16));
            File.WriteAllText(Path.Combine(directory, peer + ".crt"), certificate.ExportCertificatePem());
            File.WriteAllText(Path.Combine(directory, peer + ".key"), key.ExportPkcs8PrivateKeyPem());
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(Path.Combine(directory, peer + ".key"), UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }
    public async Task DisposeAsync()
    {
        Community?.Dispose(); Messaging?.Dispose(); Identity?.Dispose();
        if (CommunityHost is not null) await CommunityHost.DisposeAsync();
        if (messagingRunning) await MessagingHost.DisposeAsync();
        if (IdentityHost is not null) await IdentityHost.DisposeAsync();
        foreach (var settings in databases.Values) await Execute(admin, "DROP DATABASE IF EXISTS " + new NpgsqlConnectionStringBuilder(settings).Database + " WITH (FORCE)");
        Directory.Delete(directory, true);
    }
}
