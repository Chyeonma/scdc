using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SCDC.Api.Tests.Identity;
using SCDC.Contracts.Persistence;
using SCDC.Contracts.Identity;
using SCDC.Modules.Messaging.Application;

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

    [Fact]
    public async Task Forty_barrier_requests_force_unique_conflicts_and_leave_only_winning_space()
    {
        var a = f.Inner.Actor; var b = f.Inner.Users["search01"];
        var before = await f.OwnedSpaces();
        await using var gate = await f.Gate(a.Id, b.Id);
        var barrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = Enumerable.Range(0, 40).Select(async i =>
        {
            await barrier.Task;
            return await f.Open(i % 2 == 0 ? a : b, i % 2 == 0 ? b.Id : a.Id);
        }).ToArray();
        barrier.SetResult();
        await gate.WaitForBlocked(40);
        await gate.Release();
        var responses = await Task.WhenAll(tasks);
        Assert.Single(responses.Select(x => x.GetProperty("id").GetGuid()).Distinct());
        Assert.Equal("1:2", await f.PairCounts(a.Id, b.Id));
        Assert.Equal(before + 1, await f.OwnedSpaces());
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
    public async Task Guard_rechecks_security_stamp_session_owner_and_expiry_inside_shared_transaction()
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
    public async Task Database_connection_failure_in_authentication_returns_503_without_any_write()
    {
        var before = await f.OwnedSpaces();
        var original = f.Inner.Factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("Database")!;
        var unreachable = new NpgsqlConnectionStringBuilder(original) { Host = "127.0.0.1", Port = 1, Timeout = 1, Pooling = false };
        using var isolated = f.Inner.Factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Database"] = unreachable.ConnectionString })));
        using var client = isolated.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/direct-conversations");
        request.Headers.Authorization = new("Bearer", f.Inner.Actor.AccessToken);
        request.Content = JsonContent.Create(new { peerUserId = f.Inner.Users["bao"].Id });
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("AUTHORITY_UNAVAILABLE", body.GetProperty("errorCode").GetString());
        Assert.Equal(before, await f.OwnedSpaces());
    }

    [Fact]
    public async Task Both_contexts_enlist_same_physical_connection_and_transaction()
    {
        await using var scope = f.Inner.Factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var shared = services.GetRequiredService<ISharedDatabaseSession>();
        var guard = services.GetRequiredService<IAccountAccessGuard>();
        var identityType = typeof(SCDC.Modules.Identity.IdentityModule).Assembly.GetType("SCDC.Modules.Identity.Infrastructure.Persistence.IdentityDbContext")!;
        var messagingType = typeof(SCDC.Modules.Messaging.MessagingModule).Assembly.GetType("SCDC.Modules.Messaging.Infrastructure.Persistence.MessagingDbContext")!;
        var identity = (DbContext)services.GetRequiredService(identityType);
        var messaging = (DbContext)services.GetRequiredService(messagingType);
        await using var owner = await shared.BeginAsync(default);
        try
        {
            var a = f.Inner.Actor;
            var result = await guard.LockPairAsync(new(a.Id, a.SessionId, a.SecurityStamp, f.Inner.Users["bao"].Id), default);
            Assert.Null(result.Failure);
            await messaging.Database.UseTransactionAsync(shared.Transaction);
            Assert.Same(shared.Connection, identity.Database.GetDbConnection());
            Assert.Same(shared.Connection, messaging.Database.GetDbConnection());
            Assert.Same(shared.Transaction, identity.Database.CurrentTransaction!.GetDbTransaction());
            Assert.Same(shared.Transaction, messaging.Database.CurrentTransaction!.GetDbTransaction());
            var first = await identity.Database.SqlQueryRaw<string>("SELECT concat(pg_backend_pid(),':',txid_current()) AS \"Value\"").SingleAsync();
            var second = await messaging.Database.SqlQueryRaw<string>("SELECT concat(pg_backend_pid(),':',txid_current()) AS \"Value\"").SingleAsync();
            Assert.Equal(first, second);
            await owner.CommitAsync(default);
        }
        finally { await messaging.Database.UseTransactionAsync(null); await guard.DetachAsync(default); }
    }

    [Fact]
    public async Task Revoke_committed_before_guard_recheck_rejects_without_space_creation()
    {
        var a = await f.Inner.Login(f.Inner.Actor.Username); var b = f.Inner.Users["search07"];
        await using var c = await f.Connect(); await using var tx = await c.BeginTransactionAsync();
        await using (var sql = new NpgsqlCommand("SELECT id FROM identity.users WHERE id=@id FOR NO KEY UPDATE", c, tx))
        { sql.Parameters.AddWithValue("id", a.Id); await sql.ExecuteScalarAsync(); }
        await using var scope = f.Inner.Factory.Services.CreateAsyncScope();
        var opening = scope.ServiceProvider.GetRequiredService<IDirectConversationService>().OpenAsync(new(a.Id, a.SessionId, a.SecurityStamp, b.Id), default);
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
    private string Connection => Inner.Factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("Database")!;
    private string Name => Inner.Prefix + "pair_probe";
    private const long GateKey = 812347901;
    public Task InitializeAsync() => Inner.InitializeAsync();
    public async Task<NpgsqlConnection> Connect() { var c = new NpgsqlConnection(Connection); await c.OpenAsync(); return c; }
    public async Task Sql(string sql, Guid id) { await using var c = await Connect(); await using var command = new NpgsqlCommand(sql, c); command.Parameters.AddWithValue("id", id); await command.ExecuteNonQueryAsync(); }
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
