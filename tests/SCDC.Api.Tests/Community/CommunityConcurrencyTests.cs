using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace SCDC.Api.Tests.Community;

public sealed partial class CommunityApiTests
{
    private sealed class DatabaseGate(NpgsqlConnection connection, NpgsqlTransaction transaction, string trigger, string function, int key) : IAsyncDisposable
    {
        public int Key { get; } = key;
        public async Task ReleaseAsync() => await transaction.CommitAsync();
        public async ValueTask DisposeAsync()
        {
            await transaction.DisposeAsync();
            await using var cleanup = new NpgsqlCommand($"DROP TRIGGER IF EXISTS {trigger} ON {(trigger.StartsWith("test_op") ? "community.operations" : "integration.outbox_events")}; DROP FUNCTION IF EXISTS integration.{function}()", connection);
            await cleanup.ExecuteNonQueryAsync();
            await connection.DisposeAsync();
        }
    }
    private async Task<DatabaseGate> GateAsync(Guid operation, bool outbox = false)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var trigger = (outbox ? "test_outbox_" : "test_op_") + suffix;
        var function = "test_gate_" + suffix;
        var key = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, int.MaxValue);
        var condition = outbox ? $"NEW.payload->>'ownerUserId'='{_owner.UserId}'" : $"NEW.client_operation_id='{operation}'";
        await SqlAsync($"""
            CREATE FUNCTION integration.{function}() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN IF {condition} THEN PERFORM pg_advisory_xact_lock({key}::bigint); END IF; RETURN NEW; END $$;
            CREATE TRIGGER {trigger} BEFORE INSERT ON {(outbox ? "integration.outbox_events" : "community.operations")}
            FOR EACH ROW EXECUTE FUNCTION integration.{function}();
            """);
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        var transaction = await connection.BeginTransactionAsync();
        await using var acquire = new NpgsqlCommand("SELECT pg_advisory_xact_lock(@key::bigint)", connection, transaction);
        acquire.Parameters.AddWithValue("key", key);
        await acquire.ExecuteNonQueryAsync();
        return new(connection, transaction, trigger, function, key);
    }
    private async Task<int> WaitForGateAsync(DatabaseGate gate, int waiters = 1)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(1.5);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var count = await SqlAsync("SELECT count(*) FROM pg_locks WHERE locktype='advisory' AND classid=0 AND objid=@key::oid AND objsubid=1 AND NOT granted", ("key", gate.Key));
            if ((long)count! >= waiters)
                return (int)(await SqlAsync("SELECT pid FROM pg_locks WHERE locktype='advisory' AND classid=0 AND objid=@key::oid AND NOT granted LIMIT 1", ("key", gate.Key)))!;
            await Task.Delay(10);
        }
        throw new Xunit.Sdk.XunitException("Expected Community transactions to reach the controlled database gate.");
    }
    [Fact]
    public async Task Unique_operation_loser_rolls_back_and_reads_winner_from_a_new_transaction()
    {
        var operation = Guid.NewGuid();
        await using var gate = await GateAsync(operation);
        var first = CreateAsync(operation);
        await WaitForGateAsync(gate);
        var second = CreateAsync(operation);
        await WaitForGateAsync(gate, 2);
        await gate.ReleaseAsync();
        var responses = await Task.WhenAll(first, second);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        var firstId = (await JsonAsync(responses[0])).GetProperty("id").GetGuid();
        var secondId = (await JsonAsync(responses[1])).GetProperty("id").GetGuid();
        Assert.Equal(firstId, secondId);
        Assert.Equal(1L, await SqlAsync("SELECT count(*) FROM community.servers WHERE owner_user_id=@actor", ("actor", _owner.UserId)));
        Assert.Equal(1L, await SqlAsync("SELECT count(*) FROM community.server_members WHERE user_id=@actor", ("actor", _owner.UserId)));
        Assert.Equal(1L, await SqlAsync("SELECT count(*) FROM integration.outbox_events WHERE aggregate_id=@id", ("id", firstId)));
    }
    [Fact]
    public async Task Failure_after_entity_inserts_rolls_back_server_member_role_operation_and_event()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var trigger = "test_fail_" + suffix;
        var function = "test_fail_" + suffix;
        var operation = Guid.NewGuid();
        await SqlAsync($"""
            CREATE FUNCTION integration.{function}() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN IF NEW.payload->>'ownerUserId'='{_owner.UserId}' THEN RAISE EXCEPTION 'Injected outbox fault' USING ERRCODE='23514'; END IF; RETURN NEW; END $$;
            CREATE TRIGGER {trigger} BEFORE INSERT ON integration.outbox_events FOR EACH ROW EXECUTE FUNCTION integration.{function}();
            """);
        try
        {
            var failed = await CreateAsync(operation);
            Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);
            Assert.DoesNotContain("Injected outbox fault", await failed.Content.ReadAsStringAsync());
            Assert.Equal(0L, await SqlAsync("SELECT count(*) FROM community.servers WHERE owner_user_id=@actor", ("actor", _owner.UserId)));
            Assert.Equal(0L, await SqlAsync("SELECT count(*) FROM community.server_members WHERE user_id=@actor", ("actor", _owner.UserId)));
            Assert.Equal(0L, await SqlAsync("SELECT count(*) FROM community.operations WHERE actor_user_id=@actor", ("actor", _owner.UserId)));
            Assert.Equal(0L, await SqlAsync("SELECT count(*) FROM integration.outbox_events WHERE payload->>'ownerUserId'=@actor", ("actor", _owner.UserId.ToString())));
        }
        finally { await SqlAsync($"DROP TRIGGER {trigger} ON integration.outbox_events; DROP FUNCTION integration.{function}()"); }
        Assert.Equal(HttpStatusCode.Created, (await CreateAsync(operation)).StatusCode);
    }
    [Theory]
    [InlineData("logout")]
    [InlineData("password")]
    public async Task Identity_security_writer_waits_for_create_commit_then_old_token_is_rejected(string writer)
    {
        await using var gate = await GateAsync(Guid.NewGuid(), outbox: true);
        var creating = CreateAsync();
        var createPid = await WaitForGateAsync(gate);
        var changing = writer == "logout"
            ? _client.PostAsJsonAsync("/api/v1/auth/logout", new
            {
                refreshToken = _owner.RefreshToken
            })
            : SendAsync(HttpMethod.Post, "/api/v1/auth/change-password", _owner, new
            {
                currentPassword = "Initial123",
                newPassword = "Changed456"
            });
        var blocked = false;
        var deadline = DateTimeOffset.UtcNow.AddSeconds(1);
        while (DateTimeOffset.UtcNow < deadline && !changing.IsCompleted)
        {
            if (await SqlAsync("SELECT EXISTS(SELECT 1 FROM pg_stat_activity WHERE @pid=ANY(pg_blocking_pids(pid)) AND query LIKE '%FOR NO KEY UPDATE%')", ("pid", createPid)) is true)
            {
                blocked = true;
                break;
            }
            await Task.Delay(10);
        }
        Assert.True(blocked, "Identity writer must be blocked by this Community transaction's account guard.");
        await gate.ReleaseAsync();
        Assert.Equal(HttpStatusCode.Created, (await creating).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await changing).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await CreateAsync()).StatusCode);
        Assert.Equal(1L, await SqlAsync("SELECT count(*) FROM community.operations WHERE actor_user_id=@actor", ("actor", _owner.UserId)));
    }
    [Fact]
    public async Task Session_expiring_during_write_is_checked_before_commit_and_all_writes_roll_back()
    {
        var clock = new CommunityKeyTests.MutableClock(DateTimeOffset.UtcNow);
        using var clockFactory = _factory.WithWebHostBuilder(b => b.ConfigureServices(s => { s.RemoveAll<TimeProvider>(); s.AddSingleton<TimeProvider>(clock); }));
        using var client = clockFactory.CreateClient();
        await using var gate = await GateAsync(Guid.NewGuid(), outbox: true);
        var creating = CreateAsync(client: client);
        await WaitForGateAsync(gate);
        clock.Advance(TimeSpan.FromDays(31));
        await gate.ReleaseAsync();
        await AssertErrorAsync(await creating, HttpStatusCode.Unauthorized, "SESSION_INVALID");
        Assert.Equal(0L, await SqlAsync("SELECT count(*) FROM community.servers WHERE owner_user_id=@actor", ("actor", _owner.UserId)));
        Assert.Equal(0L, await SqlAsync("SELECT count(*) FROM community.operations WHERE actor_user_id=@actor", ("actor", _owner.UserId)));
        Assert.Equal(0L, await SqlAsync("SELECT count(*) FROM integration.outbox_events WHERE payload->>'ownerUserId'=@actor", ("actor", _owner.UserId.ToString())));
    }
    [Fact]
    public async Task Lock_timeout_returns_temporary_failure_without_replaying_the_mutation()
    {
        var operation = Guid.NewGuid();
        await using var gate = await GateAsync(operation);
        var creating = CreateAsync(operation);
        await WaitForGateAsync(gate);
        await AssertErrorAsync(await creating, HttpStatusCode.ServiceUnavailable, "COMMUNITY_TEMPORARILY_UNAVAILABLE");
        Assert.Equal(0L, await SqlAsync("SELECT count(*) FROM community.operations WHERE actor_user_id=@actor", ("actor", _owner.UserId)));
        await gate.ReleaseAsync();
        Assert.Equal(HttpStatusCode.Created, (await CreateAsync(operation)).StatusCode);
    }
    [Theory]
    [InlineData("owner")]
    [InlineData("everyone")]
    [InlineData("default-grant")]
    public async Task Database_rejects_invalid_owner_or_default_role_at_commit(string change)
    {
        var id = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        var sql = change switch
        {
            "owner" => "UPDATE community.server_members SET status=2,left_at=clock_timestamp() WHERE server_id=@id AND user_id=@actor",
            "everyone" => "DELETE FROM community.roles WHERE server_id=@id AND is_default",
            _ => "INSERT INTO community.permissions(code,description) VALUES('manage_invites','test') ON CONFLICT DO NOTHING; INSERT INTO community.role_permissions(role_id,permission_code) SELECT id,'manage_invites' FROM community.roles WHERE server_id=@id AND is_default"
        };
        var failure = await Assert.ThrowsAsync<PostgresException>(() => SqlAsync(sql, ("id", id), ("actor", _owner.UserId)));
        Assert.Equal("23514", failure.SqlState);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Get, $"/api/v1/servers/{id}", _owner)).StatusCode);
    }
}
