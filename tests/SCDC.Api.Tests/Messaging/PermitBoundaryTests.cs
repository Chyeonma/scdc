using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using SCDC.Api.Tests.Identity;
using SCDC.Api.Tests.Infrastructure;
using SCDC.BuildingBlocks.Infrastructure.Persistence;
using SCDC.Contracts.Identity;
using SCDC.Modules.Community.Infrastructure.Access;
using SCDC.Modules.Identity.Infrastructure.Persistence;
using SCDC.Modules.Identity.Infrastructure.Services;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Infrastructure.Access;
using SCDC.Modules.Messaging.Infrastructure.Persistence;
using SCDC.Modules.Messaging.Realtime;

namespace SCDC.Api.Tests.Messaging;

[Collection("DM database")]
public sealed class PermitBoundaryTests(RedisAccessDatabase database) : IClassFixture<RedisAccessDatabase>, IAsyncLifetime
{
    private readonly SCDCWebApplicationFactory original = new();
    private WebApplicationFactory<ApiProgram> app = null!;
    private HttpClient client = null!;
    private AuthenticatedSession actor = null!, peer = null!;
    private Func<CancellationToken, Task>? beforeProfiles;
    private Func<PermitRequest, CancellationToken, Task>? beforePermit;
    private int profileReads;
    public async Task InitializeAsync()
    {
        await database.Sql("REVOKE USAGE ON SCHEMA identity FROM scdc_messaging;");
        app = original.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(database.Configuration));
            builder.ConfigureServices(services =>
            {
                services.Replace(ServiceDescriptor.Scoped<IHistoricalUserSummaryReader>(provider =>
                    new ProfilesProbe(provider, async ct => { Interlocked.Increment(ref profileReads); if (beforeProfiles is not null) await beforeProfiles(ct); })));
                services.RemoveAllKeyed<IIdentityPermitIssuer>(AccessOwner.Messaging);
                services.AddKeyedScoped<IIdentityPermitIssuer>(AccessOwner.Messaging, (provider, _) =>
                    new PermitProbe(new IdentityPermitIssuer(provider.GetRequiredService<IConfiguration>(), AccessOwner.Messaging, TimeProvider.System),
                        async (request, ct) => { if (beforePermit is not null) await beforePermit(request, ct); }));
            });
        });
        client = app.CreateClient();
        await SCDCWebApplicationFactory.WaitForAccessReadyAsync(client);
        actor = await Register(); peer = await Register();
    }
    private async Task<AuthenticatedSession> Register()
    {
        var name = "s208_" + Guid.NewGuid().ToString("N")[..16];
        using var registered = await client.PostAsJsonAsync("/api/v1/auth/register", new { username = name, displayName = name, email = name + "@example.test", password = "DmDemo2026!Local" });
        registered.EnsureSuccessStatusCode(); var body = await registered.Content.ReadFromJsonAsync<JsonElement>();
        using var verified = await client.PostAsJsonAsync("/api/v1/auth/verify-email", new { token = body.GetProperty("developmentVerificationToken").GetString() }); verified.EnsureSuccessStatusCode();
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { login = name, password = "DmDemo2026!Local" }); login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<JsonElement>(); var jwt = new JwtSecurityTokenHandler().ReadJwtToken(auth.GetProperty("accessToken").GetString());
        string Claim(string type) => jwt.Claims.Single(c => c.Type == type).Value;
        return new(Guid.Parse(Claim("sub")), Guid.Parse(Claim("sid")), Guid.Parse(Claim("sst")), new(jwt.ValidTo, TimeSpan.Zero));
    }
    private async Task<Guid> Open()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<IDirectConversationService>().OpenAsync(new(actor.UserId, actor.SessionId, actor.SecurityStamp, peer.UserId, actor.AccessExpiresAt), default);
        Assert.True(result.IsSuccess, result.Error.Code); return result.Value.Id;
    }
    private async Task<MessageResponse> Send(Guid space, Guid operation)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<ITextMessageSender>().SendAsync(Command(space, operation), default);
        Assert.True(result.IsSuccess, result.Error.Code); return result.Value;
    }
    private SendTextMessage Command(Guid space, Guid operation) => new(actor.UserId, actor.SessionId, actor.SecurityStamp, space, operation, "original", actor.AccessExpiresAt);
    private async Task<long> Messages(Guid space) => (long)(await database.Sql("SELECT count(*) FROM messaging.messages WHERE space_id=@id", space))!;

    [Fact]
    public async Task All_DM_paths_and_realtime_work_without_Messaging_access_to_Identity_schema()
    {
        await using (var connection = new NpgsqlConnection(database.Configuration["ConnectionStrings:Messaging"]))
        {
            await connection.OpenAsync(); await using var forbidden = new NpgsqlCommand("SELECT id FROM identity.users LIMIT 1", connection);
            Assert.Equal("42501", (await Assert.ThrowsAsync<PostgresException>(() => forbidden.ExecuteScalarAsync())).SqlState);
        }
        var space = await Open(); var operation = Guid.NewGuid(); var message = await Send(space, operation);
        await using var scope = app.Services.CreateAsyncScope(); var services = scope.ServiceProvider;
        var changed = await services.GetRequiredService<IMessageMutator>().MutateAsync(new(actor.UserId, actor.SessionId, actor.SecurityStamp, space, message.Id, "1", "edited", false, actor.AccessExpiresAt), default);
        Assert.True(changed.IsSuccess, changed.Error.Code);
        var history = await services.GetRequiredService<IMessageHistory>().ReadAsync(new(actor.UserId, actor.SessionId, actor.SecurityStamp, space, 50, null, null, null, actor.AccessExpiresAt), default);
        Assert.True(history.IsSuccess, history.Error.Code); Assert.Single(history.Value.Items);
        var inbox = await services.GetRequiredService<IConversationInbox>().ListAsync(new(actor.UserId, actor.SessionId, actor.SecurityStamp, AccessExpiresAt: actor.AccessExpiresAt), default);
        Assert.True(inbox.IsSuccess, inbox.Error.Code); Assert.Single(inbox.Value.Items);
        MessageResponse? delivered = null;
        var realtime = await services.GetRequiredService<ConversationAccess>().DeliverAsync(actor, space, message.Id, (dto, _) => { delivered = dto; return Task.CompletedTask; }, default);
        Assert.True(realtime.IsSuccess, realtime.Error.Code); Assert.Equal("edited", delivered!.Content);
        Assert.Equal("2", (await Send(space, operation)).Version);
        var deleted = await services.GetRequiredService<IMessageMutator>().MutateAsync(new(actor.UserId, actor.SessionId, actor.SecurityStamp, space, message.Id, "2", null, true, actor.AccessExpiresAt), default);
        Assert.True(deleted.IsSuccess, deleted.Error.Code); Assert.Null((await Send(space, operation)).Content);
        Assert.True(profileReads >= 7);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task A_stale_account_session_or_peer_permit_rolls_back_every_local_write(int target)
    {
        var space = await Open();
        await using var scope = app.Services.CreateAsyncScope(); var services = scope.ServiceProvider;
        var gate = services.GetRequiredService<MessagingIdentityAccess>();
        var permit = await gate.AcquireAsync(actor, IdentityPurpose.DmSendNew, space, default, peer.UserId);
        var stamp = permit.Scopes.Single(s => target == 3 ? s.UserId == peer.UserId : target == 2 ? s.Kind == 2 : s.Kind == 1 && s.UserId == actor.UserId);
        var owner = app.Services.GetServices<IIdentityFenceOwner>().Single(o => o.Owner == AccessOwner.Messaging);
        await owner.ApplyAsync(new(Guid.NewGuid(), Guid.NewGuid(), 1, new[] { stamp with { Epoch = stamp.Epoch + 1 } }), default);
        var db = services.GetRequiredService<MessagingDbContext>(); var session = services.GetRequiredService<MessagingSession>();
        await using (var transaction = await session.BeginAsync(default))
        {
            try
            {
                await db.Database.UseTransactionAsync(session.Transaction);
                var failure = await Assert.ThrowsAsync<MessagingAccessFailure>(() => gate.AdmitAsync(db, permit, default)); Assert.Equal("ACCESS_CHANGED", failure.Error.Code);
            }
            finally { await db.Database.UseTransactionAsync(null); }
        }
        Assert.Equal(0, await Messages(space));
        Assert.Equal(0L, await database.Sql("SELECT count(*) FROM messaging.outbox_events WHERE space_id=@id", space));
        Assert.Equal(0L, await database.Sql("SELECT coalesce(last_message_sequence,0) FROM messaging.spaces WHERE id=@id", space));
    }

    [Fact]
    public async Task Slow_profiles_hold_no_Messaging_transaction_and_realtime_rechecks_revocation_after_the_read()
    {
        var space = await Open(); var message = await Send(space, Guid.NewGuid());
        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        beforeProfiles = async ct => { reached.TrySetResult(); await release.Task.WaitAsync(ct); };
        await using var scope = app.Services.CreateAsyncScope(); var services = scope.ServiceProvider;
        var emitted = false; var delivery = services.GetRequiredService<ConversationAccess>().DeliverAsync(actor, space, message.Id, (_, _) => { emitted = true; return Task.CompletedTask; }, default);
        await reached.Task.WaitAsync(TimeSpan.FromSeconds(5));
        try
        {
            Assert.Null(services.GetRequiredService<MessagingSession>().Transaction);
            // This account fence update must not wait for the profile lookup.
            var owner = app.Services.GetServices<IIdentityFenceOwner>().Single(o => o.Owner == AccessOwner.Messaging);
            await owner.ApplyAsync(new(Guid.NewGuid(), Guid.NewGuid(), 1, new[] { new IdentityScope(1, actor.UserId, Guid.Empty, 2) }), default).WaitAsync(TimeSpan.FromMilliseconds(500));
            await database.Sql("UPDATE identity.auth_sessions SET revoked_at=clock_timestamp() WHERE id=@id", actor.SessionId);
        }
        finally { release.TrySetResult(); }
        var result = await delivery; Assert.True(result.IsFailure); Assert.False(emitted);
    }

    [Fact]
    public async Task A_profile_read_cannot_turn_a_new_send_into_an_authorized_write_after_peer_revocation()
    {
        var space = await Open();
        beforeProfiles = async _ =>
        {
            beforeProfiles = null;
            var owner = app.Services.GetServices<IIdentityFenceOwner>().Single(o => o.Owner == AccessOwner.Messaging);
            await owner.ApplyAsync(new(Guid.NewGuid(), Guid.NewGuid(), 1, new[] { new IdentityScope(1, peer.UserId, Guid.Empty, 2) }), default);
        };
        await using var scope = app.Services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<ITextMessageSender>().SendAsync(Command(space, Guid.NewGuid()), default);
        Assert.True(result.IsFailure); Assert.Equal("ACCESS_CHANGED", result.Error.Code); Assert.Equal(0, await Messages(space));
    }

    [Fact]
    public async Task A_concurrent_committed_send_is_reclassified_as_replay_when_the_peer_becomes_unavailable()
    {
        var space = await Open(); var operation = Guid.NewGuid(); MessageResponse? winner = null;
        beforePermit = async (request, _) =>
        {
            if (request.Purpose != IdentityPurpose.DmSendNew) return;
            beforePermit = null; winner = await Send(space, operation);
            await database.Sql("UPDATE identity.users SET status=3 WHERE id=@id", peer.UserId);
        };
        var replay = await Send(space, operation); Assert.Equal(winner!.Id, replay.Id); Assert.Equal(1, await Messages(space));
    }

    [Fact]
    public async Task A_slow_network_handoff_is_cancelled_and_releases_local_fences()
    {
        var space = await Open(); var message = await Send(space, Guid.NewGuid());
        await using var scope = app.Services.CreateAsyncScope(); var services = scope.ServiceProvider;
        var observed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var result = await services.GetRequiredService<ConversationAccess>().DeliverAsync(actor, space, message.Id, async (_, ct) =>
        { try { await Task.Delay(Timeout.Infinite, ct); } catch (OperationCanceledException) { observed.TrySetResult(); throw; } }, default);
        Assert.True(result.IsFailure); await observed.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.Null(services.GetRequiredService<MessagingSession>().Transaction);
        var owner = app.Services.GetServices<IIdentityFenceOwner>().Single(o => o.Owner == AccessOwner.Messaging);
        await owner.ApplyAsync(new(Guid.NewGuid(), Guid.NewGuid(), 1, new[] { new IdentityScope(1, actor.UserId, Guid.Empty, 2) }), default).WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task A_stalled_authority_times_out_before_any_local_write()
    {
        var space = await Open(); beforePermit = (_, ct) => Task.Delay(Timeout.Infinite, ct);
        await using var scope = app.Services.CreateAsyncScope(); var services = scope.ServiceProvider;
        var result = await services.GetRequiredService<ITextMessageSender>().SendAsync(Command(space, Guid.NewGuid()), default).WaitAsync(TimeSpan.FromSeconds(4));
        Assert.True(result.IsFailure); Assert.Equal("AUTHORITY_UNAVAILABLE", result.Error.Code);
        Assert.Null(services.GetRequiredService<MessagingSession>().Transaction); Assert.Equal(0, await Messages(space));
    }

    [Theory]
    [InlineData("account")]
    [InlineData("session")]
    [InlineData("peer")]
    [InlineData("duplicate")]
    [InlineData("owner")]
    [InlineData("resource")]
    [InlineData("future")]
    public async Task A_permit_with_invalid_binding_scope_or_issue_time_cannot_enter_Messaging(string corruption)
    {
        var space = await Open(); await using var scope = app.Services.CreateAsyncScope(); var services = scope.ServiceProvider;
        var valid = await services.GetRequiredService<MessagingIdentityAccess>().AcquireAsync(actor, IdentityPurpose.DmSendNew, space, default, peer.UserId);
        var tampered = corruption switch
        {
            "account" => valid with { Scopes = valid.Scopes.Where(s => s.Kind != 1 || s.UserId != actor.UserId).ToArray() },
            "session" => valid with { Scopes = valid.Scopes.Where(s => s.Kind != 2).ToArray() },
            "peer" => valid with { Scopes = valid.Scopes.Where(s => s.UserId != peer.UserId).ToArray() },
            "duplicate" => valid with { Scopes = new[] { valid.Scopes[0], valid.Scopes[0], valid.Scopes[0] } },
            "owner" => valid with { Owner = AccessOwner.Community },
            "resource" => valid with { Request = valid.Request with { ResourceId = Guid.NewGuid() } },
            _ => valid with { IssuedAt = DateTimeOffset.UtcNow.AddMilliseconds(500) }
        };
        var gate = new MessagingIdentityAccess(new FixedPermit(tampered), TimeProvider.System);
        async Task Attempt()
        {
            var admitted = await gate.AcquireAsync(actor, IdentityPurpose.DmSendNew, space, default, peer.UserId, valid.Request.OperationId);
            var db = services.GetRequiredService<MessagingDbContext>(); var session = services.GetRequiredService<MessagingSession>();
            await using var transaction = await session.BeginAsync(default);
            try { await db.Database.UseTransactionAsync(session.Transaction); await gate.AdmitAsync(db, admitted, default); }
            finally { await db.Database.UseTransactionAsync(null); }
        }
        Assert.Equal("ACCESS_CHANGED", (await Assert.ThrowsAsync<MessagingAccessFailure>(Attempt)).Error.Code);
        Assert.Equal(0, await Messages(space));
    }

    [Fact]
    public async Task Database_clock_stops_a_nearly_expired_commit_even_if_the_application_clock_is_behind()
    {
        var space = await Open(); await using var scope = app.Services.CreateAsyncScope(); var services = scope.ServiceProvider;
        var gate = services.GetRequiredService<MessagingIdentityAccess>();
        var permit = await gate.AcquireAsync(actor, IdentityPurpose.DmSendNew, space, default, peer.UserId);
        var db = services.GetRequiredService<MessagingDbContext>(); var session = services.GetRequiredService<MessagingSession>();
        await using var transaction = await session.BeginAsync(default);
        try
        {
            await db.Database.UseTransactionAsync(session.Transaction); await gate.AdmitAsync(db, permit, default);
            var behind = new MessagingIdentityAccess(new FixedPermit(permit), new BehindClock());
            var failure = await Assert.ThrowsAsync<MessagingAccessFailure>(() => behind.EnsureAsync(db, permit with { ExpiresAt = DateTimeOffset.UtcNow.AddMilliseconds(100) }, default));
            Assert.Equal("ACCESS_CHANGED", failure.Error.Code);
        }
        finally { await db.Database.UseTransactionAsync(null); }
        Assert.Equal(0, await Messages(space));
    }

    [Theory]
    [InlineData(-100)]
    [InlineData(100)]
    public async Task Opposite_clock_offsets_within_the_budget_allow_concurrent_commits(int authorityOffsetMs)
    {
        var space = await Open();
        using var skewed = app.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAllKeyed<IIdentityPermitIssuer>(AccessOwner.Messaging);
            services.AddKeyedScoped<IIdentityPermitIssuer>(AccessOwner.Messaging, (provider, _) =>
                new IdentityPermitIssuer(provider.GetRequiredService<IConfiguration>(), AccessOwner.Messaging, new OffsetClock(authorityOffsetMs)));
            services.Replace(ServiceDescriptor.Scoped<MessagingIdentityAccess>(provider =>
                new(provider.GetRequiredKeyedService<IIdentityPermitIssuer>(AccessOwner.Messaging), new OffsetClock(-authorityOffsetMs))));
        }));
        var services = skewed.Services;
        var responses = await Task.WhenAll(Enumerable.Range(0, 16).Select(async _ =>
        {
            await using var scope = services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<ITextMessageSender>().SendAsync(Command(space, Guid.NewGuid()), default);
        }));
        Assert.All(responses, result => Assert.True(result.IsSuccess, result.Error.Code));
        Assert.Equal(16, await Messages(space));
        Assert.Equal(16L, await database.Sql("SELECT count(*) FROM messaging.outbox_events WHERE space_id=@id", space));
        Assert.Equal(16L, await database.Sql("SELECT last_message_sequence FROM messaging.spaces WHERE id=@id", space));
    }

    private sealed class OffsetClock(int offsetMs) : TimeProvider
    { public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow.AddMilliseconds(offsetMs); }

    [Theory]
    [InlineData(-100)]
    [InlineData(100)]
    public async Task Community_clock_offsets_admit_a_valid_lease_and_DB_rejects_expiry(int authorityOffsetMs)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var issuer = new IdentityPermitIssuer(services.GetRequiredService<IConfiguration>(), AccessOwner.Community, new OffsetClock(authorityOffsetMs));
        var gate = new CommunityIdentityAccess(issuer, new OffsetClock(-authorityOffsetMs));
        var permit = await gate.AcquireAsync(new(actor.UserId, actor.SessionId, actor.SecurityStamp, actor.AccessExpiresAt),
            IdentityPurpose.CommunityWrite, Guid.NewGuid(), Guid.NewGuid(), default);
        var factory = services.GetRequiredService<RelationalWorkScopeFactory>();
        await using (var work = await factory.OpenAsync(default))
        {
            var lease = await gate.AdmitAsync(work, permit, default);
            await gate.EnsureAsync(work, lease, default);
            await work.CommitAsync(default);
        }
        await using (var work = await factory.OpenAsync(default))
        {
            var lease = await gate.AdmitAsync(work, permit, default);
            var behind = new CommunityIdentityAccess(issuer, new OffsetClock(-1000));
            var expired = lease with { Permit = permit with { ExpiresAt = DateTimeOffset.UtcNow.AddMilliseconds(100) } };
            Assert.Equal("ACCESS_CHANGED", (await Assert.ThrowsAsync<AccessCheckException>(() => behind.EnsureAsync(work, expired, default))).Code);
        }
    }
    public async Task DisposeAsync() { client.Dispose(); await app.DisposeAsync(); await original.DisposeAsync(); }
    private sealed class ProfilesProbe(IServiceProvider services, Func<CancellationToken, Task> before) : IHistoricalUserSummaryReader
    {
        public async Task<IReadOnlyDictionary<Guid, UserSummary>> ReadAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
        {
            Assert.Null(services.GetRequiredService<IdentityDbContext>().Database.CurrentTransaction);
            Assert.Null(services.GetRequiredService<MessagingSession>().Transaction);
            await before(ct); return await new HistoricalUserSummaryReader(services.GetRequiredService<IdentityDbContext>()).ReadAsync(ids, ct);
        }
    }
    private sealed class PermitProbe(IIdentityPermitIssuer inner, Func<PermitRequest, CancellationToken, Task> before) : IIdentityPermitIssuer
    {
        public async Task<IdentityPermit> AcquireAsync(PermitRequest request, CancellationToken ct) { await before(request, ct); return await inner.AcquireAsync(request, ct); }
    }
    private sealed class FixedPermit(IdentityPermit permit) : IIdentityPermitIssuer
    { public Task<IdentityPermit> AcquireAsync(PermitRequest request, CancellationToken ct) => Task.FromResult(permit); }
    private sealed class BehindClock : TimeProvider
    { public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow.AddSeconds(-1); }
}
