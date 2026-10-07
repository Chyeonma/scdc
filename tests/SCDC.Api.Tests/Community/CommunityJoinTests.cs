using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using SCDC.Api.Tests.Infrastructure;

namespace SCDC.Api.Tests.Community;

public sealed partial class CommunityApiTests
{
    private Task<HttpResponseMessage> JoinAsync(Guid id, Actor? actor = null, HttpClient? client = null)
        => SendAsync(HttpMethod.Post, $"/api/v1/servers/{id}/join", actor ?? _other, client: client);
    private Task<object?> JoinedEventsAsync(Guid id)
        => SqlAsync("SELECT count(*) FROM integration.outbox_events WHERE aggregate_id=@id AND event_type='Community.MembershipJoined.v1'", ("id", id));
    private Task<object?> OtherMembershipsAsync(Guid id)
        => SqlAsync("SELECT count(*) FROM community.server_members WHERE server_id=@id AND user_id=@actor", ("id", id), ("actor", _other.UserId));

    [Fact]
    public async Task Join_commits_default_membership_and_one_event_and_active_retries_survive_restart()
    {
        var id = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        var response = await JoinAsync(id);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var membership = await JsonAsync(response);
        var epoch = membership.GetProperty("membershipId").GetGuid();
        Assert.Equal(7, epoch.Version);
        Assert.Equal(_other.UserId, membership.GetProperty("userId").GetGuid());
        Assert.Equal(id, membership.GetProperty("serverId").GetGuid());
        Assert.Equal("active", membership.GetProperty("status").GetString());
        Assert.Equal("1", membership.GetProperty("version").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, membership.GetProperty("leftAt").ValueKind);
        var detail = await JsonAsync(await SendAsync(HttpMethod.Get, $"/api/v1/servers/{id}", _other));
        Assert.Equal("2", detail.GetProperty("version").GetString());
        Assert.Equal("2", detail.GetProperty("accessVersion").GetString());
        Assert.Empty(detail.GetProperty("effectivePermissions").EnumerateArray());
        Assert.Equal(epoch, detail.GetProperty("myMembership").GetProperty("membershipId").GetGuid());
        var list = await JsonAsync(await SendAsync(HttpMethod.Get, "/api/v1/servers", _other));
        Assert.Single(list.GetProperty("items").EnumerateArray());
        await using var restarted = new SCDCWebApplicationFactory();
        using var client = restarted.CreateClient();
        Assert.Equal(epoch, (await JsonAsync(await JoinAsync(id, client: client))).GetProperty("membershipId").GetGuid());
        Assert.Equal(1L, await OtherMembershipsAsync(id));
        Assert.Equal(1L, await JoinedEventsAsync(id));
        Assert.Equal(1L, await SqlAsync("""
            SELECT count(*) FROM integration.outbox_events WHERE aggregate_id=@id AND event_type='Community.MembershipJoined.v1'
              AND aggregate_version=2 AND published_at IS NULL AND payload->>'membershipId'=@epoch
              AND payload->>'membershipVersion'='1' AND payload->>'accessVersion'='2'
            """, ("id", id), ("epoch", epoch.ToString())));
        await SqlAsync("UPDATE community.servers SET join_mode=2 WHERE id=@id", ("id", id));
        Assert.Equal(epoch, (await JsonAsync(await JoinAsync(id))).GetProperty("membershipId").GetGuid());
        Assert.Equal(1L, await JoinedEventsAsync(id));
        Assert.Equal(2, await SqlAsync("SELECT access_version FROM community.servers WHERE id=@id", ("id", id)));
    }

    [Theory]
    [InlineData("private")]
    [InlineData("inactive")]
    [InlineData("deleted")]
    [InlineData("unknown")]
    public async Task Join_hides_unavailable_servers_without_creating_membership(string change)
    {
        var id = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        if (change == "private") await SqlAsync("UPDATE community.servers SET visibility=2 WHERE id=@id", ("id", id));
        if (change == "inactive") await SqlAsync("UPDATE community.servers SET status=2 WHERE id=@id", ("id", id));
        if (change == "deleted") await SqlAsync("UPDATE community.servers SET status=3,deleted_at=clock_timestamp() WHERE id=@id", ("id", id));
        if (change == "unknown") id = Guid.CreateVersion7();
        var response = await JoinAsync(id);
        await AssertErrorAsync(response, HttpStatusCode.NotFound, "RESOURCE_NOT_FOUND");
        Assert.DoesNotContain("Test community", await response.Content.ReadAsStringAsync());
        Assert.Equal(0L, await OtherMembershipsAsync(id));
        Assert.Equal(0L, await JoinedEventsAsync(id));
    }

    [Fact]
    public async Task Join_rejects_approval_and_body_supplied_actor_without_granting_access()
    {
        var id = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        await AssertErrorAsync(await SendAsync(HttpMethod.Post, $"/api/v1/servers/{id}/join", _other,
            new { userId = _owner.UserId, roleId = Guid.CreateVersion7() }), HttpStatusCode.BadRequest, "VALIDATION_FAILED");
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PostAsync($"/api/v1/servers/{id}/join", null)).StatusCode);
        await SqlAsync("UPDATE community.servers SET join_mode=2 WHERE id=@id", ("id", id));
        await AssertErrorAsync(await JoinAsync(id), HttpStatusCode.Conflict, "JOIN_APPROVAL_REQUIRED");
        Assert.Equal(0L, await OtherMembershipsAsync(id));
        Assert.Equal(0L, await JoinedEventsAsync(id));
        Assert.Equal(1, await SqlAsync("SELECT access_version FROM community.servers WHERE id=@id", ("id", id)));
    }

    private async Task<Guid> LeftWithGrantsAsync(Guid id)
    {
        var epoch = Guid.CreateVersion7();
        await SqlAsync("""
            INSERT INTO community.server_members(server_id,user_id,membership_id,status,version,joined_at,left_at,nickname,timeout_until,invited_by_user_id)
              VALUES(@id,@actor,@epoch,2,4,clock_timestamp()-interval '1 day',clock_timestamp(),'old nickname',clock_timestamp()+interval '1 day',@owner);
            INSERT INTO community.roles(id,server_id,name,name_key) VALUES(@role,@id,'Old grant','old grant');
            INSERT INTO community.permissions(code,description) VALUES('manage_invites','test'),('channel_view','test') ON CONFLICT DO NOTHING;
            INSERT INTO community.role_permissions(role_id,permission_code) VALUES(@role,'manage_invites');
            INSERT INTO community.member_roles(server_id,user_id,role_id,membership_id) VALUES(@id,@actor,@role,@epoch);
            INSERT INTO messaging.spaces(id,space_type) VALUES(@channel,3);
            INSERT INTO community.channels(space_id,server_id,name) VALUES(@channel,@id,'old-room');
            INSERT INTO community.channel_user_overrides(space_id,server_id,user_id,permission_code,effect,membership_id) VALUES(@channel,@id,@actor,'channel_view',1,@epoch);
            """, ("id", id), ("actor", _other.UserId), ("epoch", epoch), ("owner", _owner.UserId), ("role", Guid.CreateVersion7()), ("channel", Guid.CreateVersion7()));
        return epoch;
    }

    [Fact]
    public async Task Rejoin_rotates_epoch_and_removes_old_assignments_overrides_and_legacy_fields()
    {
        var id = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        var previous = await LeftWithGrantsAsync(id);
        var result = await JsonAsync(await JoinAsync(id));
        Assert.NotEqual(previous, result.GetProperty("membershipId").GetGuid());
        Assert.Equal("5", result.GetProperty("version").GetString());
        Assert.Equal("active", result.GetProperty("status").GetString());
        Assert.True(result.GetProperty("joinedAt").GetDateTimeOffset() > DateTimeOffset.UtcNow.AddMinutes(-1));
        Assert.Equal(0L, await SqlAsync("SELECT count(*) FROM community.member_roles WHERE server_id=@id AND user_id=@actor", ("id", id), ("actor", _other.UserId)));
        Assert.Equal(0L, await SqlAsync("SELECT count(*) FROM community.channel_user_overrides WHERE server_id=@id AND user_id=@actor", ("id", id), ("actor", _other.UserId)));
        Assert.Equal(true, await SqlAsync("SELECT nickname IS NULL AND timeout_until IS NULL AND invited_by_user_id IS NULL AND left_at IS NULL FROM community.server_members WHERE server_id=@id AND user_id=@actor", ("id", id), ("actor", _other.UserId)));
        var detail = await JsonAsync(await SendAsync(HttpMethod.Get, $"/api/v1/servers/{id}", _other));
        Assert.Empty(detail.GetProperty("effectivePermissions").EnumerateArray());
        Assert.Equal(1L, await JoinedEventsAsync(id));
    }

    [Theory]
    [InlineData("unverified", HttpStatusCode.Forbidden)]
    [InlineData("inactive", HttpStatusCode.Unauthorized)]
    [InlineData("revoked", HttpStatusCode.Unauthorized)]
    public async Task Join_checks_current_account_and_session(string state, HttpStatusCode status)
    {
        var id = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        var sql = state switch
        {
            "unverified" => "UPDATE identity.user_emails SET verified_at=NULL WHERE user_id=@actor",
            "inactive" => "UPDATE identity.users SET status=2 WHERE id=@actor",
            _ => "UPDATE identity.auth_sessions SET revoked_at=clock_timestamp() WHERE id=@session"
        };
        await SqlAsync(sql, ("actor", _other.UserId), ("session", _other.SessionId));
        Assert.Equal(status, (await JoinAsync(id)).StatusCode);
        Assert.Equal(0L, await OtherMembershipsAsync(id));
        Assert.Equal(0L, await JoinedEventsAsync(id));
    }

    private async Task<DatabaseGate> JoinGateAsync(Guid id)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var trigger = "test_join_" + suffix;
        var function = "test_join_gate_" + suffix;
        var key = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, int.MaxValue);
        await SqlAsync($"""
            CREATE FUNCTION integration.{function}() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN IF NEW.event_type='Community.MembershipJoined.v1' AND NEW.aggregate_id='{id}' THEN
              PERFORM pg_advisory_xact_lock({key}::bigint); END IF; RETURN NEW; END $$;
            CREATE TRIGGER {trigger} BEFORE INSERT ON integration.outbox_events FOR EACH ROW EXECUTE FUNCTION integration.{function}();
            """);
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        var transaction = await connection.BeginTransactionAsync();
        await using var acquire = new NpgsqlCommand("SELECT pg_advisory_xact_lock(@key::bigint)", connection, transaction);
        acquire.Parameters.AddWithValue("key", key);
        await acquire.ExecuteNonQueryAsync();
        return new(connection, transaction, trigger, function, key);
    }

    private async Task WaitForBlockedQueryAsync(int blocker, string pattern)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(1.5);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await SqlAsync("SELECT EXISTS(SELECT 1 FROM pg_stat_activity WHERE @pid=ANY(pg_blocking_pids(pid)) AND query LIKE @pattern)", ("pid", blocker), ("pattern", pattern)) is true) return;
            await Task.Delay(10);
        }
        throw new Xunit.Sdk.XunitException("Expected operation to wait for the controlled server/account lock.");
    }

    [Fact]
    public async Task Concurrent_join_waits_for_server_lock_then_returns_the_committed_epoch_without_another_event()
    {
        var id = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        await using var gate = await JoinGateAsync(id);
        var first = JoinAsync(id);
        var pid = await WaitForGateAsync(gate);
        var second = JoinAsync(id);
        await WaitForBlockedQueryAsync(pid, "%FROM community.servers WHERE id=%FOR UPDATE%");
        await gate.ReleaseAsync();
        var responses = await Task.WhenAll(first, second);
        foreach (var response in responses) Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal((await JsonAsync(responses[0])).GetProperty("membershipId").GetGuid(), (await JsonAsync(responses[1])).GetProperty("membershipId").GetGuid());
        Assert.Equal(1L, await OtherMembershipsAsync(id));
        Assert.Equal(1L, await JoinedEventsAsync(id));
        Assert.Equal(2, await SqlAsync("SELECT access_version FROM community.servers WHERE id=@id", ("id", id)));
    }

    [Theory]
    [InlineData("visibility", HttpStatusCode.NotFound)]
    [InlineData("join_mode", HttpStatusCode.Conflict)]
    public async Task Join_reads_mode_and_visibility_after_waiting_for_a_concurrent_update(string field, HttpStatusCode status)
    {
        var id = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var lockServer = new NpgsqlCommand($"SELECT pg_backend_pid() FROM community.servers WHERE id='{id}' FOR UPDATE", connection, transaction);
        var blocker = (int)(await lockServer.ExecuteScalarAsync())!;
        var joining = JoinAsync(id);
        await WaitForBlockedQueryAsync(blocker, "%FROM community.servers WHERE id=%FOR UPDATE%");
        await using var change = new NpgsqlCommand($"UPDATE community.servers SET {field}=2 WHERE id='{id}'", connection, transaction);
        await change.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
        Assert.Equal(status, (await joining).StatusCode);
        Assert.Equal(0L, await OtherMembershipsAsync(id));
        Assert.Equal(0L, await JoinedEventsAsync(id));
    }

    [Fact]
    public async Task Outbox_fault_rolls_back_rejoin_cleanup_membership_and_server_versions()
    {
        var id = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        var epoch = await LeftWithGrantsAsync(id);
        var name = "test_join_fail_" + Guid.NewGuid().ToString("N");
        await SqlAsync($"""
            CREATE FUNCTION integration.{name}() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN IF NEW.event_type='Community.MembershipJoined.v1' AND NEW.aggregate_id='{id}' THEN
              RAISE EXCEPTION 'Injected join fault' USING ERRCODE='23514'; END IF; RETURN NEW; END $$;
            CREATE TRIGGER {name} BEFORE INSERT ON integration.outbox_events FOR EACH ROW EXECUTE FUNCTION integration.{name}();
            """);
        try
        {
            var failed = await JoinAsync(id);
            Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);
            Assert.DoesNotContain("Injected join fault", await failed.Content.ReadAsStringAsync());
            var membership = await JsonAsync(await SendAsync(HttpMethod.Get, $"/api/v1/servers/{id}/membership/me", _other));
            Assert.Equal(epoch, membership.GetProperty("membershipId").GetGuid());
            Assert.Equal("left", membership.GetProperty("status").GetString());
            Assert.Equal("4", membership.GetProperty("version").GetString());
            Assert.Equal(1L, await SqlAsync("SELECT count(*) FROM community.member_roles WHERE server_id=@id AND user_id=@actor", ("id", id), ("actor", _other.UserId)));
            Assert.Equal(1L, await SqlAsync("SELECT count(*) FROM community.channel_user_overrides WHERE server_id=@id AND user_id=@actor", ("id", id), ("actor", _other.UserId)));
            Assert.Equal(true, await SqlAsync("SELECT version=1 AND access_version=1 FROM community.servers WHERE id=@id", ("id", id)));
            Assert.Equal(0L, await JoinedEventsAsync(id));
        }
        finally { await SqlAsync($"DROP TRIGGER {name} ON integration.outbox_events; DROP FUNCTION integration.{name}()"); }
        Assert.Equal(HttpStatusCode.OK, (await JoinAsync(id)).StatusCode);
        Assert.Equal(1L, await JoinedEventsAsync(id));
    }

    [Theory]
    [InlineData("access")]
    [InlineData("membership")]
    public async Task Join_prevents_version_overflow_without_writes(string field)
    {
        var id = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        if (field == "access") await SqlAsync("UPDATE community.servers SET access_version=2147483647 WHERE id=@id", ("id", id));
        else await SqlAsync("INSERT INTO community.server_members(server_id,user_id,status,left_at,version) VALUES(@id,@actor,2,clock_timestamp(),2147483647)", ("id", id), ("actor", _other.UserId));
        await AssertErrorAsync(await JoinAsync(id), HttpStatusCode.Conflict, "VERSION_LIMIT_REACHED");
        Assert.Equal(0L, await JoinedEventsAsync(id));
        Assert.Equal(0L, await SqlAsync("SELECT count(*) FROM community.server_members WHERE server_id=@id AND user_id=@actor AND status=1", ("id", id), ("actor", _other.UserId)));
    }

    [Fact]
    public async Task Join_expiry_before_commit_rolls_back_and_lock_timeout_does_not_replay()
    {
        var id = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        var clock = new CommunityKeyTests.MutableClock(DateTimeOffset.UtcNow);
        using var clockFactory = _factory.WithWebHostBuilder(b => b.ConfigureServices(s => { s.RemoveAll<TimeProvider>(); s.AddSingleton<TimeProvider>(clock); }));
        using var client = clockFactory.CreateClient();
        await using (var gate = await JoinGateAsync(id))
        {
            var joining = JoinAsync(id, client: client);
            await WaitForGateAsync(gate);
            clock.Advance(TimeSpan.FromDays(31));
            await gate.ReleaseAsync();
            await AssertErrorAsync(await joining, HttpStatusCode.Unauthorized, "SESSION_INVALID");
        }
        Assert.Equal(0L, await OtherMembershipsAsync(id));
        Assert.Equal(0L, await JoinedEventsAsync(id));
        Assert.Equal(true, await SqlAsync("SELECT version=1 AND access_version=1 FROM community.servers WHERE id=@id", ("id", id)));
        await using (var gate = await JoinGateAsync(id))
        {
            var joining = JoinAsync(id);
            await WaitForGateAsync(gate);
            await AssertErrorAsync(await joining, HttpStatusCode.ServiceUnavailable, "COMMUNITY_TEMPORARILY_UNAVAILABLE");
            Assert.Equal(0L, await OtherMembershipsAsync(id));
            Assert.Equal(0L, await JoinedEventsAsync(id));
            await gate.ReleaseAsync();
        }
    }

    [Fact]
    public async Task Logout_waits_for_join_guard_then_old_session_is_rejected()
    {
        var id = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        await using var gate = await JoinGateAsync(id);
        var joining = JoinAsync(id);
        var pid = await WaitForGateAsync(gate);
        var logout = _client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = _other.RefreshToken });
        await WaitForBlockedQueryAsync(pid, "%FOR NO KEY UPDATE%");
        await gate.ReleaseAsync();
        Assert.Equal(HttpStatusCode.OK, (await joining).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await logout).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await JoinAsync(id)).StatusCode);
        Assert.Equal(1L, await JoinedEventsAsync(id));
    }
}
