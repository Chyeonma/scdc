using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Npgsql;
using SCDC.Api.Tests.Identity;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Infrastructure.Security;

namespace SCDC.Api.Tests.Messaging;

[Collection("DM database")]
public sealed class TextMessageTests(TextMessageFixture f) : IClassFixture<TextMessageFixture>
{
    [Fact]
    public async Task Entire_pinned_corpus_is_checked_by_real_HTTP_and_Postgres()
    {
        var space = await f.Open("search01");
        using var corpus = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures/text-validation.json")));
        var valid = 0;
        foreach (var item in corpus.RootElement.GetProperty("cases").EnumerateArray())
        {
            var expected = item.GetProperty("expectedValid").GetBoolean();
            var id = Guid.NewGuid();
            var raw = "{\"clientMessageId\":\"" + id + "\",\"content\":" + item.GetProperty("content").GetRawText() + "}";
            var body = await f.SendRaw(space, f.D.Inner.Actor, raw, expected ? 200 : 400);
            if (expected)
            {
                valid++;
                Assert.Equal(item.GetProperty("expectedNormalizedContent").GetString(), body.GetProperty("content").GetString());
                Assert.Equal(valid.ToString(), body.GetProperty("sequence").GetString());
                Assert.Equal("1", body.GetProperty("version").GetString());
            }
            else Assert.StartsWith("CONTENT_", body.GetProperty("errorCode").GetString());
        }
        Assert.Equal($"{valid}:{valid}:{valid}:{valid}", await f.Counts(space));
    }

    [Fact]
    public void Hmac_matches_all_six_interoperability_vectors()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures/dm-fingerprint.json")));
        var root = document.RootElement;
        foreach (var item in root.GetProperty("cases").EnumerateArray())
        {
            var text = TextContent.Validate(item.GetProperty("inputContent").GetString()).Content!;
            var hash = MessageFingerprint.Hash(Convert.FromHexString(root.GetProperty("syntheticKeyHex").GetString()!),
                root.GetProperty("spaceId").GetGuid(), root.GetProperty("authorId").GetGuid(), root.GetProperty("clientMessageId").GetGuid(), text);
            Assert.Equal(item.GetProperty("expectedFingerprintHex").GetString(), Convert.ToHexStringLower(hash));
        }
    }

    [Fact]
    public async Task Forty_simultaneous_identical_operations_commit_once_and_conflict_is_immutable()
    {
        var space = await f.Open("search02"); var id = Guid.NewGuid();
        var replies = await Task.WhenAll(Enumerable.Range(0, 40).Select(_ => f.Send(space, f.D.Inner.Actor, id, "  a\r\nb  ")));
        Assert.Single(replies.Select(x => x.GetProperty("id").GetGuid()).Distinct());
        Assert.All(replies, x => Assert.Equal("  a\nb  ", x.GetProperty("content").GetString()));
        Assert.Equal("1:1:1:1", await f.Counts(space));
        var conflict = await f.Send(space, f.D.Inner.Actor, id, "different", 409);
        Assert.Equal("OPERATION_CONFLICT", conflict.GetProperty("errorCode").GetString());
        await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), "  a\nb  ");
        Assert.Equal("2:2:2:2", await f.Counts(space));
        Assert.Equal("0", await f.Scalar("SELECT count(*)::text FROM integration.outbox_events WHERE space_id=@id AND (payload ? 'content' OR payload ? 'body')", space));
        Assert.Equal("0", await f.Scalar("SELECT count(*)::text FROM messaging.message_edits e JOIN messaging.messages m ON m.id=e.message_id WHERE m.space_id=@id", space));
    }

    [Fact]
    public async Task Opposite_direction_new_operations_have_unique_contiguous_sequences()
    {
        var space = await f.Open("search03");
        var replies = await Task.WhenAll(Enumerable.Range(0, 20).Select(i => f.Send(space,
            i % 2 == 0 ? f.D.Inner.Actor : f.D.Inner.Users["search03"], Guid.NewGuid(), "simultaneous")));
        Assert.Equal(Enumerable.Range(1, 20).Select(i => (long)i), replies.Select(x => long.Parse(x.GetProperty("sequence").GetString()!)).Order());
        Assert.Equal("20:20:20:20", await f.Counts(space));
        var last = replies.Single(x => x.GetProperty("sequence").GetString() == "20");
        Assert.Equal(last.GetProperty("id").GetGuid().ToString(), await f.Scalar("SELECT last_message_id::text FROM messaging.spaces WHERE id=@id", space));
    }

    [Fact]
    public async Task Replay_reads_current_edited_deleted_dto_even_when_peer_is_disabled()
    {
        var space = await f.Open("search04"); var id = Guid.NewGuid();
        var original = await f.Send(space, f.D.Inner.Actor, id, "original");
        await f.D.Sql("UPDATE messaging.messages SET content='edited',version=2,edited_at=created_at WHERE space_id=@id", space);
        await f.D.Sql("UPDATE identity.users SET status=2 WHERE id=@id", f.D.Inner.Users["search04"].Id);
        var edited = await f.Send(space, f.D.Inner.Actor, id, "original");
        Assert.Equal("edited", edited.GetProperty("content").GetString()); Assert.Equal("2", edited.GetProperty("version").GetString());
        Assert.Equal(original.GetProperty("id").GetGuid(), edited.GetProperty("id").GetGuid());
        await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), "new", 404);
        await f.D.Sql("UPDATE messaging.messages SET content=NULL,version=3,deleted_at=created_at WHERE space_id=@id", space);
        var deleted = await f.Send(space, f.D.Inner.Actor, id, "original");
        Assert.Equal(JsonValueKind.Null, deleted.GetProperty("content").ValueKind); Assert.Equal("3", deleted.GetProperty("version").GetString());
        Assert.Equal("1:1:1:1", await f.Counts(space));
    }

    [Fact]
    public async Task Outsider_anonymous_revoked_and_invalid_uuid_do_not_write()
    {
        var space = await f.Open("search05");
        await f.Send(space, f.D.Inner.Users["chi"], Guid.NewGuid(), "private", 404);
        await f.Send(space, null, Guid.NewGuid(), "private", 401);
        await f.Send(space, f.D.Inner.Actor, Guid.CreateVersion7(), "private", 400);
        var revoked = await f.D.Inner.Login(f.D.Inner.Actor.Username);
        await f.D.Sql("UPDATE identity.auth_sessions SET revoked_at=now(),revoke_reason='text-test' WHERE id=@id", revoked.SessionId);
        await f.Send(space, revoked, Guid.NewGuid(), "private", 401);
        Assert.Equal("0:0:0:0", await f.Counts(space));
    }

    [Fact]
    public async Task Outbox_insert_failure_rolls_back_message_operation_counter_and_activity()
    {
        var space = await f.Open("search06");
        var before = await f.Scalar("SELECT coalesce(last_activity_at::text,'null') FROM messaging.spaces WHERE id=@id", space);
        try
        {
            await f.Fault(space, true);
            var rejected = await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), "rollback", 503);
            Assert.Equal("AUTHORITY_UNAVAILABLE", rejected.GetProperty("errorCode").GetString());
            Assert.Equal("0:0:0:0", await f.Counts(space));
            Assert.Equal(before, await f.Scalar("SELECT coalesce(last_activity_at::text,'null') FROM messaging.spaces WHERE id=@id", space));
        }
        finally { await f.Fault(space, false); }
        await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), "after rollback");
        Assert.Equal("1:1:1:1", await f.Counts(space));
    }

    [Fact]
    public async Task Missing_key_and_legacy_unverifiable_operation_fail_closed_without_new_write()
    {
        var space = await f.Open("search07"); var id = Guid.NewGuid();
        await f.Send(space, f.D.Inner.Actor, id, "original");
        using var isolated = f.D.Inner.Factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["Modules:Messaging:FingerprintKeyDirectory"] = "/no-such-text-test-key-directory" })));
        using var client = isolated.CreateClient();
        foreach (var operation in new[] { id, Guid.NewGuid() })
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/direct-conversations/{space}/messages")
                { Content = JsonContent.Create(new { clientMessageId = operation, content = "original" }) };
            request.Headers.Authorization = new("Bearer", f.D.Inner.Actor.AccessToken);
            using var response = await client.SendAsync(request);
            Assert.Equal(503, (int)response.StatusCode);
            Assert.Equal("FINGERPRINT_KEY_UNAVAILABLE", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString());
        }
        await f.D.Sql("UPDATE messaging.send_operations SET fingerprint_version=NULL,key_id=NULL,fingerprint=NULL WHERE space_id=@id", space);
        var legacy = await f.Send(space, f.D.Inner.Actor, id, "original", 409);
        Assert.Equal("OPERATION_UNVERIFIABLE", legacy.GetProperty("errorCode").GetString());
        Assert.Equal("1:1:1:1", await f.Counts(space));
    }

    [Fact]
    public async Task Sequence_above_javascript_safe_integer_is_exact_decimal_string()
    {
        var space = await f.Open("search08");
        await f.D.Sql("UPDATE messaging.spaces SET last_message_sequence=9007199254740992 WHERE id=@id", space);
        var sent = await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), "bigint");
        Assert.Equal("9007199254740993", sent.GetProperty("sequence").GetString());
    }

    [Fact]
    public async Task Rotation_keeps_old_operation_key_and_missing_old_key_cannot_create_a_duplicate()
    {
        var space = await f.Open("search09"); var id = Guid.NewGuid();
        var directory = Path.Combine(Path.GetTempPath(), "scdc-p2-keys-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        await File.WriteAllBytesAsync(Path.Combine(directory, "old.key"), Enumerable.Range(0, 32).Select(i => (byte)i).ToArray());
        await File.WriteAllBytesAsync(Path.Combine(directory, "new.key"), Enumerable.Range(32, 32).Select(i => (byte)i).ToArray());
        try
        {
            foreach (var key in new[] { "old", "new" })
            {
                using var host = f.D.Inner.Factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string?> { ["Modules:Messaging:FingerprintKeyDirectory"] = directory,
                        ["Modules:Messaging:ActiveFingerprintKeyId"] = key })));
                using var client = host.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/direct-conversations/{space}/messages")
                    { Content = JsonContent.Create(new { clientMessageId = id, content = "original" }) };
                request.Headers.Authorization = new("Bearer", f.D.Inner.Actor.AccessToken);
                using var response = await client.SendAsync(request); Assert.Equal(200, (int)response.StatusCode);
            }
            Assert.Equal("old", await f.Scalar("SELECT key_id FROM messaging.send_operations WHERE space_id=@id", space));
            File.Delete(Path.Combine(directory, "old.key"));
            using var missing = f.D.Inner.Factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["Modules:Messaging:FingerprintKeyDirectory"] = directory,
                    ["Modules:Messaging:ActiveFingerprintKeyId"] = "new" })));
            using var missingClient = missing.CreateClient();
            using var retry = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/direct-conversations/{space}/messages")
                { Content = JsonContent.Create(new { clientMessageId = id, content = "original" }) };
            retry.Headers.Authorization = new("Bearer", f.D.Inner.Actor.AccessToken);
            using var unavailable = await missingClient.SendAsync(retry); Assert.Equal(503, (int)unavailable.StatusCode);
            Assert.Equal("FINGERPRINT_KEY_UNAVAILABLE", (await unavailable.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString());
            Assert.Equal("1:1:1:1", await f.Counts(space));
        }
        finally { File.Delete(Path.Combine(directory, "old.key")); File.Delete(Path.Combine(directory, "new.key")); Directory.Delete(directory); }
    }

    [Fact]
    public async Task Send_auth_database_failure_is_503_and_ineligible_actor_is_401()
    {
        var space = await f.Open("search10");
        var original = f.D.Inner.Factory.Services.GetService(typeof(IConfiguration)) as IConfiguration;
        var unreachable = new NpgsqlConnectionStringBuilder(original!.GetConnectionString("Database")!)
            { Host = "127.0.0.1", Port = 1, Timeout = 1, Pooling = false };
        using var host = f.D.Inner.Factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Database"] = unreachable.ConnectionString })));
        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/direct-conversations/{space}/messages")
            { Content = JsonContent.Create(new { clientMessageId = Guid.NewGuid(), content = "must not write" }) };
        request.Headers.Authorization = new("Bearer", f.D.Inner.Actor.AccessToken);
        using var response = await client.SendAsync(request); Assert.Equal(503, (int)response.StatusCode);
        Assert.Equal("AUTHORITY_UNAVAILABLE", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString());
        var actor = f.D.Inner.Users["search10"];
        await f.D.Sql("UPDATE identity.user_emails SET verified_at=NULL WHERE user_id=@id AND is_primary", actor.Id);
        await f.Send(space, actor, Guid.NewGuid(), "unverified", 401);
        await f.D.Sql("UPDATE identity.user_emails SET verified_at=now() WHERE user_id=@id AND is_primary", actor.Id);
        await f.D.Sql("UPDATE identity.users SET status=3 WHERE id=@id", actor.Id);
        await f.Send(space, actor, Guid.NewGuid(), "disabled", 401);
        Assert.Equal("0:0:0:0", await f.Counts(space));
    }

    [Fact]
    public async Task Writer_held_before_commit_serializes_next_writer_without_sequence_gaps()
    {
        var space = await f.Open("search11"); const long key = 892741305;
        await using var gate = await f.D.Connect();
        await using (var hold = new NpgsqlCommand($"SELECT pg_advisory_lock({key})", gate)) await hold.ExecuteNonQueryAsync();
        try
        {
            await f.Fault(space, true, $"PERFORM pg_advisory_xact_lock({key});");
            var first = f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), "first held at outbox");
            await f.WaitForLockChain(gate.ProcessID, 1);
            var second = f.Send(space, f.D.Inner.Users["search11"], Guid.NewGuid(), "second waits for commit");
            await f.WaitForLockChain(gate.ProcessID, 2);
            Assert.Equal("0:0:0:0", await f.Counts(space));
            await using (var release = new NpgsqlCommand($"SELECT pg_advisory_unlock({key})", gate)) await release.ExecuteNonQueryAsync();
            var replies = await Task.WhenAll(first, second);
            Assert.Equal(new[] { "1", "2" }, replies.Select(x => x.GetProperty("sequence").GetString()).ToArray());
            Assert.Equal("2:2:2:2", await f.Counts(space));
        }
        finally
        {
            await using (var release = new NpgsqlCommand($"SELECT pg_advisory_unlock({key})", gate)) await release.ExecuteNonQueryAsync();
            await f.Fault(space, false);
        }
    }
}

public sealed class TextMessageFixture : IAsyncLifetime
{
    public DirectFixture D { get; } = new();
    private string Trigger => D.Inner.Prefix + "text_fault";
    public Task InitializeAsync() => D.InitializeAsync();
    public async Task<Guid> Open(string alias) => (await D.Open(D.Inner.Actor, D.Inner.Users[alias].Id)).GetProperty("id").GetGuid();
    public Task<JsonElement> Send(Guid space, UserSearchFixture.ActorData? actor, Guid id, string content, int expected = 200)
        => SendRaw(space, actor, JsonSerializer.Serialize(new { clientMessageId = id, content }), expected);
    public async Task<JsonElement> SendRaw(Guid space, UserSearchFixture.ActorData? actor, string raw, int expected)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/direct-conversations/{space}/messages")
            { Content = new StringContent(raw, Encoding.UTF8, "application/json") };
        if (actor is not null) request.Headers.Authorization = new("Bearer", actor.AccessToken);
        using var response = await D.Inner.Client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True((int)response.StatusCode == expected, $"Expected {expected}, got {(int)response.StatusCode}: {body}");
        return body;
    }
    public async Task<string> Scalar(string query, Guid id)
    {
        await using var c = await D.Connect(); await using var sql = new NpgsqlCommand(query, c);
        sql.Parameters.AddWithValue("id", id); return (string)(await sql.ExecuteScalarAsync())!;
    }
    public Task<string> Counts(Guid space) => Scalar("""
        SELECT concat((SELECT count(*) FROM messaging.messages WHERE space_id=@id),':',
        (SELECT count(*) FROM messaging.send_operations WHERE space_id=@id),':',
        (SELECT count(*) FROM integration.outbox_events WHERE space_id=@id),':',last_message_sequence)
        FROM messaging.spaces WHERE id=@id
        """, space);
    public async Task Fault(Guid space, bool enabled, string action = "RAISE EXCEPTION USING ERRCODE='58000', MESSAGE='Scoped acceptance outbox fault';")
    {
        await using var c = await D.Connect();
        var query = $"DROP TRIGGER IF EXISTS {Trigger} ON integration.outbox_events; DROP FUNCTION IF EXISTS integration.{Trigger}();";
        if (enabled) query += $"""
            CREATE FUNCTION integration.{Trigger}() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN IF NEW.space_id='{space}' THEN {action} END IF; RETURN NEW; END $$;
            CREATE TRIGGER {Trigger} BEFORE INSERT ON integration.outbox_events FOR EACH ROW EXECUTE FUNCTION integration.{Trigger}();
            """;
        await using var sql = new NpgsqlCommand(query, c); await sql.ExecuteNonQueryAsync();
    }
    public async Task WaitForLockChain(int pid, int expected)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            await using var c = await D.Connect();
            await using var sql = new NpgsqlCommand("""
                WITH RECURSIVE waiting(pid) AS (
                  SELECT pid FROM pg_stat_activity WHERE @pid=ANY(pg_blocking_pids(pid))
                  UNION SELECT a.pid FROM pg_stat_activity a JOIN waiting w ON w.pid=ANY(pg_blocking_pids(a.pid))
                ) SELECT count(*) FROM waiting
                """, c);
            sql.Parameters.AddWithValue("pid", pid);
            if ((long)(await sql.ExecuteScalarAsync())! >= expected) return;
            await Task.Delay(25);
        }
        Assert.Fail($"Expected {expected} database writers in the held commit lock chain.");
    }
    public async Task DisposeAsync()
    {
        await Fault(Guid.Empty, false);
        await using (var c = await D.Connect())
        {
            await using var sql = new NpgsqlCommand("""
                DELETE FROM integration.outbox_events WHERE space_id IN (SELECT id FROM messaging.spaces WHERE created_by_user_id=ANY(@ids));
                DELETE FROM messaging.send_operations WHERE space_id IN (SELECT id FROM messaging.spaces WHERE created_by_user_id=ANY(@ids));
                DELETE FROM messaging.messages WHERE space_id IN (SELECT id FROM messaging.spaces WHERE created_by_user_id=ANY(@ids));
                """, c);
            sql.Parameters.AddWithValue("ids", D.Inner.Users.Values.Select(x => x.Id).ToArray()); await sql.ExecuteNonQueryAsync();
        }
        await D.DisposeAsync();
    }
}
