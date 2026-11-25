using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SCDC.Api.Tests.Identity;
using SCDC.Contracts.Persistence;
using SCDC.Contracts.Identity;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Api.Tests.Messaging;

[CollectionDefinition("DM database", DisableParallelization = true)]
public sealed class DmDatabaseCollection;

[Collection("DM database")]
public sealed class DirectConversationTests(DirectFixture f) : IClassFixture<DirectFixture>
{
    [Fact]
    public async Task Opens_exact_pair_and_reuses_UUIDv7_in_both_directions()
    {
        var a = f.Inner.Actor; var b = f.Inner.Users["bao"];
        var first = await f.Open(a, b.Id);
        var reverse = await f.Open(b, a.Id);
        Assert.Equal(first.GetProperty("id").GetGuid(), reverse.GetProperty("id").GetGuid());
        Assert.Equal('7', first.GetProperty("id").GetString()![14]);
        Assert.Equal("0", first.GetProperty("lastSequence").GetString());
        Assert.Equal(JsonValueKind.Null, first.GetProperty("lastActivityAt").ValueKind);
        Assert.Equal(2, first.GetProperty("participants").GetArrayLength());
        foreach (var person in first.GetProperty("participants").EnumerateArray())
            Assert.Equal(new[] { "displayName", "id", "username" }, person.EnumerateObject().Select(x => x.Name).Order());
        Assert.Equal("1:2", await f.PairCounts(a.Id, b.Id));
    }

    [Fact]
    public async Task Validation_and_ineligible_peers_create_nothing()
    {
        var a = f.Inner.Actor;
        foreach (var peer in new[] { Guid.Empty, a.Id, Guid.NewGuid(), f.Inner.Users["pending"].Id,
                     f.Inner.Users["disabled"].Id, f.Inner.Users["deleted"].Id, f.Inner.Users["unverified"].Id })
        {
            var status = peer == Guid.Empty || peer == a.Id ? 400 : 404;
            var body = await f.Open(a, peer, status);
            Assert.Equal(peer == Guid.Empty ? "Common.ValidationFailed" : peer == a.Id ? "INVALID_PEER" : "RESOURCE_NOT_FOUND",
                body.GetProperty("errorCode").GetString());
            Assert.Equal("0:0", await f.PairCounts(a.Id, peer));
        }
        foreach (var body in new object[] { new { }, new { peerUserId = "not-a-uuid" } })
        {
            using var response = await f.Inner.Send("/api/v1/direct-conversations", a, body);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        using var anon = await f.Inner.Client.PostAsJsonAsync("/api/v1/direct-conversations", new { peerUserId = f.Inner.Users["bao"].Id });
        Assert.Equal(HttpStatusCode.Unauthorized, anon.StatusCode);
    }

    [Fact]
    public async Task Existing_pair_retains_public_unavailable_peer_but_disabled_actor_cannot_open()
    {
        var a = f.Inner.Actor; var b = f.Inner.Users["editable"];
        var original = await f.Open(a, b.Id);
        await f.Sql("UPDATE identity.users SET status=3 WHERE id=@id", b.Id);
        try
        {
            var reopened = await f.Open(a, b.Id);
            Assert.Equal(original.GetProperty("id").GetGuid(), reopened.GetProperty("id").GetGuid());
            Assert.Equal("unavailable", reopened.GetProperty("participants").EnumerateArray().Single(p => p.GetProperty("id").GetGuid() == b.Id).GetProperty("availability").GetString());
            await f.Open(b, a.Id, 401);
        }
        finally { await f.Sql("UPDATE identity.users SET status=1 WHERE id=@id", b.Id); }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1500)]
    public async Task Forty_concurrent_requests_leave_only_one_space(int lastRequestDelayMs)
    {
        var a = f.Inner.Actor; var b = f.Inner.Users[lastRequestDelayMs == 0 ? "search01" : "search09"];
        // Existing fence rows allow concurrent readers. First-time fence insertion is deliberately serialized.
        foreach (var actor in new[] { a, b })
        {
            using var warm = await f.Inner.Send("/api/v1/direct-conversations", actor);
            Assert.Equal(HttpStatusCode.OK, warm.StatusCode);
        }
        var before = await f.OwnedSpaces();
        var barrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = Enumerable.Range(0, 40).Select(async i =>
        {
            await barrier.Task;
            // Arrival skew must not hold the other requests inside their 500 ms lock budget.
            if (i == 39) await Task.Delay(lastRequestDelayMs);
            return await f.Open(i % 2 == 0 ? a : b, i % 2 == 0 ? b.Id : a.Id);
        }).ToArray();
        barrier.SetResult();
        var responses = await Task.WhenAll(tasks);
        Assert.Single(responses.Select(x => x.GetProperty("id").GetGuid()).Distinct());
        Assert.Equal("1:2", await f.PairCounts(a.Id, b.Id));
        Assert.Equal(before + 1, await f.OwnedSpaces());
    }

    [Fact]
    public async Task Unique_pair_conflict_rolls_back_losing_space_and_reopens_winner()
    {
        var a = f.Inner.Actor; var b = f.Inner.Users["search08"];
        foreach (var actor in new[] { a, b })
        {
            using var warm = await f.Inner.Send("/api/v1/direct-conversations", actor);
            Assert.Equal(HttpStatusCode.OK, warm.StatusCode);
        }
        var before = await f.OwnedSpaces();
        var race = new UniquePairConflictProbe(() => f.Open(b, a.Id));
        using var app = f.Inner.Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddDbContext<MessagingDbContext>(options => options.AddInterceptors(race))));
        using var client = app.CreateClient();
        await SCDC.Api.Tests.Infrastructure.SCDCWebApplicationFactory.WaitForAccessReadyAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/direct-conversations")
        { Content = JsonContent.Create(new { peerUserId = b.Id }) };
        request.Headers.Authorization = new("Bearer", a.AccessToken);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, race.UniqueConflicts);
        Assert.Equal(race.Winner.GetProperty("id").GetGuid(), body.GetProperty("id").GetGuid());
        Assert.Equal("1:2", await f.PairCounts(a.Id, b.Id));
        Assert.Equal(before + 1, await f.OwnedSpaces());
    }

    private sealed class UniquePairConflictProbe(Func<Task<JsonElement>> createWinner) : DbCommandInterceptor
    {
        private int started;
        public JsonElement Winner { get; private set; }
        public int UniqueConflicts { get; private set; }

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            // The loser has inserted its space and checked the pair. Commit a real competing
            // HTTP request now so PostgreSQL must reject this INSERT with the pair constraint.
            if (command.CommandText.Contains("INSERT INTO messaging.direct_conversations", StringComparison.Ordinal)
                && Interlocked.CompareExchange(ref started, 1, 0) == 0)
                Winner = await createWinner();
            return result;
        }

        public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Exception is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ux_direct_conversations_pair" })
                UniqueConflicts++;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Failure_after_space_insert_rolls_back_and_manual_retry_creates_once()
    {
        var a = f.Inner.Actor; var b = f.Inner.Users["search02"];
        var before = await f.OwnedSpaces();
        await f.SetTrigger(a.Id, b.Id, "RAISE EXCEPTION USING ERRCODE='58000', MESSAGE='Acceptance database fault';");
        try
        {
            var result = await f.Open(a, b.Id, 503);
            Assert.Equal("AUTHORITY_UNAVAILABLE", result.GetProperty("errorCode").GetString());
            Assert.Equal(before, await f.OwnedSpaces());
            Assert.Equal("0:0", await f.PairCounts(a.Id, b.Id));
        }
        finally { await f.RemoveTrigger(); }
        await f.Open(a, b.Id);
        Assert.Equal(before + 1, await f.OwnedSpaces());
        Assert.Equal("1:2", await f.PairCounts(a.Id, b.Id));
    }

    [Fact]
    public async Task Logout_waits_for_guard_transaction_then_revoked_token_cannot_write()
    {
        var a = await f.Inner.Login(f.Inner.Actor.Username); var b = f.Inner.Users["search03"];
        await using var gate = await f.Gate(a.Id, b.Id);
        var opening = f.Open(a, b.Id);
        await gate.WaitForBlocked(1);
        var logout = f.Inner.Send("/api/v1/auth/logout", a, new { refreshToken = a.RefreshToken });
        await Task.Delay(150);
        Assert.False(logout.IsCompleted);
        await gate.Release();
        await opening;
        using var response = await logout;
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await f.Open(a, f.Inner.Users["search04"].Id, 401);
        Assert.Equal("0:0", await f.PairCounts(a.Id, f.Inner.Users["search04"].Id));
    }

    [Fact]
    public async Task Permit_rechecks_security_stamp_session_owner_and_expiry_before_local_transaction()
    {
        var a = f.Inner.Actor; var b = f.Inner.Users["search05"];
        foreach (var command in new[] {
            new OpenDirectConversation(a.Id, a.SessionId, Guid.NewGuid(), b.Id),
            new OpenDirectConversation(a.Id, b.SessionId, a.SecurityStamp, b.Id) })
        {
            await using var scope = f.Inner.Factory.Services.CreateAsyncScope();
            var result = await scope.ServiceProvider.GetRequiredService<IDirectConversationService>().OpenAsync(command, default);
            Assert.True(result.IsFailure);
            Assert.Equal("Common.Unauthorized", result.Error.Code);
        }
        var expired = await f.Inner.Login(a.Username);
        await f.Sql("UPDATE identity.auth_sessions SET created_at=now()-interval '2 days', expires_at=now()-interval '1 day' WHERE id=@id", expired.SessionId);
        await f.Open(expired, b.Id, 401);
        Assert.Equal("0:0", await f.PairCounts(a.Id, b.Id));
    }

    [Fact]
    public async Task Redis_connection_failure_in_authentication_returns_503_without_any_write()
    {
        var before = await f.OwnedSpaces();
        using var isolated = f.Inner.Factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["Authentication:Sessions:Endpoint"] = "127.0.0.1:1" })));
        using var client = isolated.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/direct-conversations");
        request.Headers.Authorization = new("Bearer", f.Inner.Actor.AccessToken);
        request.Content = JsonContent.Create(new { peerUserId = f.Inner.Users["bao"].Id });
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ACCESS_CHECK_UNAVAILABLE", body.GetProperty("errorCode").GetString());
        Assert.Equal(before, await f.OwnedSpaces());
    }

    [Fact]
    public async Task Messaging_keeps_a_local_connection_and_never_enlists_the_identity_context()
    {
        await using var scope = f.Inner.Factory.Services.CreateAsyncScope(); var services = scope.ServiceProvider;
        var local = services.GetRequiredService<SCDC.Modules.Messaging.Infrastructure.Persistence.MessagingSession>();
        var access = services.GetRequiredService<SCDC.Modules.Messaging.Infrastructure.Access.MessagingIdentityAccess>();
        var identity = services.GetRequiredService<SCDC.Modules.Identity.Infrastructure.Persistence.IdentityDbContext>();
        var messaging = services.GetRequiredService<SCDC.Modules.Messaging.Infrastructure.Persistence.MessagingDbContext>();
        var actor = f.Inner.Actor;
        var permit = await access.AcquireAsync(new(actor.Id, actor.SessionId, actor.SecurityStamp, actor.AccessExpiresAt), IdentityPurpose.DmRead, actor.Id, default);
        await using var owner = await local.BeginAsync(default);
        try
        {
            await messaging.Database.UseTransactionAsync(local.Transaction);
            await access.AdmitAsync(messaging, permit, default);
            Assert.NotSame(identity.Database.GetDbConnection(), messaging.Database.GetDbConnection());
            Assert.Same(local.Connection, messaging.Database.GetDbConnection());
            Assert.Null(identity.Database.CurrentTransaction);
            Assert.Same(local.Transaction, messaging.Database.CurrentTransaction!.GetDbTransaction());
            await access.EnsureAsync(messaging, permit, default); await owner.CommitAsync(default);
        }
        finally { await messaging.Database.UseTransactionAsync(null); }
    }

    [Fact]
    public async Task Revoke_committed_before_guard_recheck_rejects_without_space_creation()
    {
        var a = await f.Inner.Login(f.Inner.Actor.Username); var b = f.Inner.Users["search07"];
        await using var c = await f.Connect(); await using var tx = await c.BeginTransactionAsync();
        await using (var sql = new NpgsqlCommand("SELECT id FROM identity.users WHERE id=@id FOR NO KEY UPDATE", c, tx))
        { sql.Parameters.AddWithValue("id", a.Id); await sql.ExecuteScalarAsync(); }
        await using var scope = f.Inner.Factory.Services.CreateAsyncScope();
        var opening = scope.ServiceProvider.GetRequiredService<IDirectConversationService>().OpenAsync(new(a.Id, a.SessionId, a.SecurityStamp, b.Id, a.AccessExpiresAt), default);
        var deadline = DateTime.UtcNow.AddSeconds(10); var blocked = false;
        while (DateTime.UtcNow < deadline)
        {
            await using var probe = await f.Connect();
            await using var sql = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_stat_activity WHERE @pid=ANY(pg_blocking_pids(pid)))", probe);
            sql.Parameters.AddWithValue("pid", c.ProcessID);
            if ((bool)(await sql.ExecuteScalarAsync())!) { blocked = true; break; }
            await Task.Delay(25);
        }
        Assert.True(blocked);
        await using (var sql = new NpgsqlCommand("UPDATE identity.auth_sessions SET revoked_at=now(),revoke_reason='acceptance-revoke-first' WHERE id=@id", c, tx))
        { sql.Parameters.AddWithValue("id", a.SessionId); await sql.ExecuteNonQueryAsync(); }
        await tx.CommitAsync();
        var result = await opening;
        Assert.True(result.IsFailure); Assert.Equal("Common.Unauthorized", result.Error.Code);
        Assert.Equal("0:0", await f.PairCounts(a.Id, b.Id));
    }

    [Fact]
    public async Task UUID_network_order_matches_PostgreSQL_when_little_endian_order_differs()
    {
        var a = Guid.Parse("01000000-0000-7000-8000-000000000001");
        var b = Guid.Parse("00000001-0000-7000-8000-000000000002");
        Assert.True(a.ToByteArray().AsSpan().SequenceCompareTo(b.ToByteArray()) < 0);
        Assert.True(UuidNetworkOrder.Compare(a, b) > 0);
        await using var c = await f.Connect();
        await using var sql = new NpgsqlCommand("SELECT @a::uuid > @b::uuid", c);
        sql.Parameters.AddWithValue("a", a); sql.Parameters.AddWithValue("b", b);
        Assert.True((bool)(await sql.ExecuteScalarAsync())!);
    }

    [Fact]
    public async Task Database_rejects_orphan_space_and_third_member_at_commit()
    {
        var a = f.Inner.Actor; var b = f.Inner.Users["search06"];
        var pair = await f.Open(a, b.Id); var space = pair.GetProperty("id").GetGuid();
        foreach (var mutation in new[] {
            $"INSERT INTO messaging.spaces(id,space_type,created_by_user_id) VALUES ('{Guid.CreateVersion7()}',1,'{a.Id}')",
            $"INSERT INTO messaging.space_members(space_id,user_id) VALUES ('{space}','{f.Inner.Users["chi"].Id}')",
            $"DELETE FROM messaging.space_members WHERE space_id='{space}' AND user_id='{b.Id}'" })
        {
            await using var c = await f.Connect(); await using var tx = await c.BeginTransactionAsync();
            await using var sql = new NpgsqlCommand(mutation, c, tx); await sql.ExecuteNonQueryAsync();
            var error = await Assert.ThrowsAsync<PostgresException>(async () => await tx.CommitAsync());
            Assert.Equal("23514", error.SqlState); Assert.Equal("ck_dm_exact_pair_members", error.ConstraintName);
        }
        Assert.Equal("1:2", await f.PairCounts(a.Id, b.Id));
    }
}

public sealed class DirectFixture : IAsyncLifetime
{
    public UserSearchFixture Inner { get; } = new();
    private string Connection => Inner.Factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("TestDatabase")!;
    private string Name => Inner.Prefix + "pair_probe";
    private const long GateKey = 812347901;
    public Task InitializeAsync() => Inner.InitializeAsync();
    public async Task<NpgsqlConnection> Connect() { var c = new NpgsqlConnection(Connection); await c.OpenAsync(); return c; }
    public async Task Sql(string sql, Guid id)
    {
        await using var c = await Connect(); await using var command = new NpgsqlCommand(sql, c);
        command.Parameters.AddWithValue("id", id); await command.ExecuteNonQueryAsync();
        if (sql.Contains("identity.", StringComparison.Ordinal))
            await Inner.Factory.Services.GetRequiredService<SCDC.Modules.Identity.Infrastructure.Services.SessionPublisher>().SynchronizeAsync(default);
    }
    public async Task<JsonElement> Open(UserSearchFixture.ActorData actor, Guid peer, int expected = 200)
    {
        using var response = await Inner.Send("/api/v1/direct-conversations", actor, new { peerUserId = peer });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expected, (int)response.StatusCode); return body;
    }
    public async Task<string> PairCounts(Guid a, Guid b)
    {
        var pair = UuidNetworkOrder.Pair(a, b); await using var c = await Connect();
        await using var sql = new NpgsqlCommand("SELECT concat(count(*),':',coalesce(sum((SELECT count(*) FROM messaging.space_members m WHERE m.space_id=d.space_id)),0)) FROM messaging.direct_conversations d WHERE user_low_id=@a AND user_high_id=@b", c);
        sql.Parameters.AddWithValue("a", pair.Low); sql.Parameters.AddWithValue("b", pair.High);
        return (string)(await sql.ExecuteScalarAsync())!;
    }
    public async Task<long> OwnedSpaces()
    {
        await using var c = await Connect(); await using var sql = new NpgsqlCommand("SELECT count(*) FROM messaging.spaces WHERE created_by_user_id=ANY(@ids)", c);
        sql.Parameters.AddWithValue("ids", Inner.Users.Values.Select(x => x.Id).ToArray()); return (long)(await sql.ExecuteScalarAsync())!;
    }
    public async Task SetTrigger(Guid a, Guid b, string action)
    {
        var pair = UuidNetworkOrder.Pair(a, b); await using var c = await Connect();
        await using var sql = new NpgsqlCommand($"""
            CREATE OR REPLACE FUNCTION messaging.{Name}() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN IF NEW.user_low_id='{pair.Low}' AND NEW.user_high_id='{pair.High}' THEN {action} END IF; RETURN NEW; END $$;
            DROP TRIGGER IF EXISTS {Name} ON messaging.direct_conversations;
            CREATE TRIGGER {Name} BEFORE INSERT ON messaging.direct_conversations FOR EACH ROW EXECUTE FUNCTION messaging.{Name}();
            """, c); await sql.ExecuteNonQueryAsync();
    }
    public async Task RemoveTrigger()
    {
        await using var c = await Connect(); await using var sql = new NpgsqlCommand($"DROP TRIGGER IF EXISTS {Name} ON messaging.direct_conversations; DROP FUNCTION IF EXISTS messaging.{Name}();", c); await sql.ExecuteNonQueryAsync();
    }
    public async Task<DatabaseGate> Gate(Guid a, Guid b)
    {
        var c = await Connect(); await using var command = new NpgsqlCommand($"SELECT pg_advisory_lock({GateKey})", c); await command.ExecuteNonQueryAsync();
        await SetTrigger(a, b, $"PERFORM pg_advisory_xact_lock({GateKey});"); return new(this, c, GateKey);
    }
    public async Task DisposeAsync()
    {
        await RemoveTrigger();
        await using (var c = await Connect())
        { await using var sql = new NpgsqlCommand("DELETE FROM messaging.spaces WHERE created_by_user_id=ANY(@ids)", c); sql.Parameters.AddWithValue("ids", Inner.Users.Values.Select(x => x.Id).ToArray()); await sql.ExecuteNonQueryAsync(); }
        await Inner.DisposeAsync();
    }
    public sealed class DatabaseGate(DirectFixture fixture, NpgsqlConnection connection, long key) : IAsyncDisposable
    {
        private bool released;
        public async Task WaitForBlocked(int count)
        {
            var until = DateTime.UtcNow.AddSeconds(20);
            while (DateTime.UtcNow < until)
            {
                await using var c = await fixture.Connect(); await using var sql = new NpgsqlCommand($"SELECT count(*) FROM pg_locks WHERE locktype='advisory' AND objid={key} AND NOT granted", c);
                if ((long)(await sql.ExecuteScalarAsync())! >= count) return;
                await Task.Delay(25);
            }
            Assert.Fail($"Expected {count} real database requests blocked after space insertion.");
        }
        public async Task Release() { if (released) return; await using var sql = new NpgsqlCommand($"SELECT pg_advisory_unlock({key})", connection); await sql.ExecuteNonQueryAsync(); released = true; }
        public async ValueTask DisposeAsync() { await Release(); await connection.DisposeAsync(); await fixture.RemoveTrigger(); }
    }
}
