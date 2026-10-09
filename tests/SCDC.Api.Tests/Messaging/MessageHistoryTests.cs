using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Npgsql;
using SCDC.Api.Tests.Identity;

namespace SCDC.Api.Tests.Messaging;

[Collection("DM database")]
public sealed class MessageHistoryTests(TextMessageFixture f) : IClassFixture<TextMessageFixture>
{
    private async Task<JsonElement> Page(Guid space, string query = "", UserSearchFixture.ActorData? actor = null, int expected = 200, bool anonymous = false)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/direct-conversations/{space}/messages{query}");
        if (!anonymous) request.Headers.Authorization = new("Bearer", (actor ?? f.D.Inner.Actor).AccessToken);
        using var response = await f.D.Inner.Client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True((int)response.StatusCode == expected, $"Expected {expected}, got {(int)response.StatusCode}"); return body;
    }
    private static string? Cursor(JsonElement page, string field = "nextCursor") => page.GetProperty(field).GetString();
    private static string Before(string cursor, int limit = 50) => $"?limit={limit}&before={Uri.EscapeDataString(cursor)}";
    private static string After(string cursor, int limit = 50) => $"?limit={limit}&after={Uri.EscapeDataString(cursor)}";
    private static string[] Ids(JsonElement page) => page.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetString()!).ToArray();

    [Fact]
    public async Task H121_latest_before_are_50_50_21_ascending_same_database_IDs_and_read_only()
    {
        var space = await f.Open("bao"); var written = new List<string>();
        for (var i = 1; i <= 121; i++) written.Add((await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), $"HIST-{i:000}")).GetProperty("id").GetString()!);
        var counts = await f.Counts(space); var first = await Page(space, actor: f.D.Inner.Users["bao"]);
        var second = await Page(space, Before(Cursor(first)!), f.D.Inner.Users["bao"]);
        var third = await Page(space, Before(Cursor(second)!), f.D.Inner.Users["bao"]);
        Assert.Equal(written.Skip(71), Ids(first)); Assert.Equal(written.Skip(21).Take(50), Ids(second)); Assert.Equal(written.Take(21), Ids(third));
        foreach (var page in new[] { first, second, third }) Assert.Equal("121", page.GetProperty("throughSequence").GetString());
        Assert.True(first.GetProperty("hasMore").GetBoolean()); Assert.True(second.GetProperty("hasMore").GetBoolean()); Assert.False(third.GetProperty("hasMore").GetBoolean());
        Assert.NotNull(Cursor(first, "resumeCursor")); Assert.Null(Cursor(second, "resumeCursor")); Assert.Null(Cursor(third));
        Assert.Equal(counts, await f.Counts(space));
    }

    [Fact]
    public async Task Catchup_seals_new_frontier_once_and_resume_advances_only_after_final_page()
    {
        var space = await f.Open("search01"); var latest = await Page(space); Assert.Empty(Ids(latest));
        for (var i = 1; i <= 5; i++) await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), $"new-{i}");
        // A protected resume is bound to its original page size, so bootstrap a size-2 cursor.
        var zero = await Page(space, "?limit=2&after=0");
        Assert.Equal("5", zero.GetProperty("throughSequence").GetString()); Assert.Null(Cursor(zero, "resumeCursor"));
        await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), "outside sealed frontier");
        var second = await Page(space, After(Cursor(zero)!, 2) + "&through=5"); var final = await Page(space, After(Cursor(second)!, 2));
        Assert.Equal("5", final.GetProperty("throughSequence").GetString()); Assert.Single(Ids(final)); Assert.NotNull(Cursor(final, "resumeCursor"));
        var newRound = await Page(space, After(Cursor(final, "resumeCursor")!, 2));
        Assert.Single(Ids(newRound)); Assert.Equal("6", newRound.GetProperty("throughSequence").GetString());
        var fromResume = await Page(space, After(Cursor(latest, "resumeCursor")!)); Assert.Equal(6, Ids(fromResume).Length);
    }

    [Fact]
    public async Task Cursor_scope_actor_direction_limit_tamper_frontier_and_invalid_combinations_are_rejected()
    {
        var space = await f.Open("search02"); var other = await f.Open("search03");
        for (var i = 0; i < 3; i++) await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), "cursor");
        var page = await Page(space, "?limit=1"); var before = Cursor(page)!; var resume = Cursor(page, "resumeCursor")!;
        foreach (var query in new[] { Before(before, 2), After(before, 1), Before(resume, 1), "?before=", "?after=42", "?before=junk",
            Before(before[..^8] + "broken!!", 1), After(resume, 1) + "&through=4", "?after=" + new string('x', 4097) })
            Assert.Equal("CURSOR_INVALID", (await Page(space, query, expected: 400)).GetProperty("errorCode").GetString());
        await Page(space, Before(before, 1), f.D.Inner.Users["search02"], 400);
        await Page(other, Before(before, 1), expected: 400);
        foreach (var query in new[] { "?before=a&after=b", "?before=a&through=3", "?through=3", "?after=0&through=3", "?limit=0", "?limit=101" })
            Assert.Equal("Common.ValidationFailed", (await Page(space, query, expected: 400)).GetProperty("errorCode").GetString());
        Assert.Equal("RESOURCE_NOT_FOUND", (await Page(space, Before(before, 1), f.D.Inner.Users["chi"], 404)).GetProperty("errorCode").GetString());
        await Page(Guid.NewGuid(), expected: 404); await Page(space, anonymous: true, expected: 401);
        Assert.Equal(3, Ids(await Page(space, "?limit=100")).Length);
    }

    [Fact]
    public async Task Cursor_survives_new_host_keys_and_expiry_still_checks_current_session()
    {
        var space = await f.Open("search04"); for (var i = 0; i < 2; i++) await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), "restart");
        var cursor = Cursor(await Page(space, "?limit=1"))!;
        using var host = f.D.Inner.MakeFactory(); using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/direct-conversations/{space}/messages" + Before(cursor, 1));
        request.Headers.Authorization = new("Bearer", f.D.Inner.Actor.AccessToken);
        using var response = await client.SendAsync(request); Assert.Equal(200, (int)response.StatusCode);
        f.D.Inner.Clock.CursorFuture = true;
        try { await Page(space, Before(cursor, 1), expected: 400); } finally { f.D.Inner.Clock.CursorFuture = false; }
        var peer = f.D.Inner.Users["search04"];
        await f.D.Sql("UPDATE identity.auth_sessions SET revoked_at=now() WHERE user_id=@id", peer.Id);
        await Page(space, actor: peer, expected: 401);
    }

    [Fact]
    public async Task Current_edit_tombstone_unavailable_peer_and_bigint_are_read_without_mutation()
    {
        var space = await f.Open("search05");
        await f.D.Sql("UPDATE messaging.spaces SET last_message_sequence=9007199254740991 WHERE id=@id", space);
        var first = await f.Send(space, f.D.Inner.Users["search05"], Guid.NewGuid(), "edit original");
        var second = await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), "delete original");
        await f.D.Sql("UPDATE messaging.messages SET content='current',edited_at=now(),version=10 WHERE id=@id", first.GetProperty("id").GetGuid());
        await f.D.Sql("UPDATE messaging.messages SET content=NULL,deleted_at=now(),version=11 WHERE id=@id", second.GetProperty("id").GetGuid());
        await f.D.Sql("UPDATE identity.users SET status=3 WHERE id=@id", f.D.Inner.Users["search05"].Id);
        var counts = await f.Counts(space); var page = await Page(space); var items = page.GetProperty("items");
        Assert.Equal("9007199254740993", page.GetProperty("throughSequence").GetString());
        Assert.Equal("9007199254740992", items[0].GetProperty("sequence").GetString()); Assert.Equal("current", items[0].GetProperty("content").GetString());
        Assert.Equal("10", items[0].GetProperty("version").GetString()); Assert.Null(items[1].GetProperty("content").GetString());
        Assert.Equal("unavailable", items[0].GetProperty("author").GetProperty("availability").GetString());
        Assert.Equal(counts, await f.Counts(space));
    }

    [Fact]
    public async Task Reader_waits_for_held_commit_and_rollback_does_not_leave_a_hidden_lower_sequence()
    {
        var space = await f.Open("search06"); const long key = 892741309;
        await using var gate = await f.D.Connect();
        await using (var hold = new NpgsqlCommand($"SELECT pg_advisory_lock({key})", gate)) await hold.ExecuteNonQueryAsync();
        try
        {
            await f.Fault(space, true, $"PERFORM pg_advisory_xact_lock({key});");
            var writer = f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), "held"); await f.WaitForLockChain(gate.ProcessID, 1);
            var reader = Page(space); await f.WaitForLockChain(gate.ProcessID, 2); Assert.False(reader.IsCompleted);
            await using (var release = new NpgsqlCommand($"SELECT pg_advisory_unlock({key})", gate)) await release.ExecuteNonQueryAsync();
            var committed = await writer; var page = await reader; Assert.Equal(new[] { committed.GetProperty("id").GetString() }, Ids(page));
            Assert.Equal("1", page.GetProperty("throughSequence").GetString());
            await f.Fault(space, true); await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), "rollback", 503);
            Assert.Single(Ids(await Page(space))); Assert.Equal("1:1:1:1", await f.Counts(space));
        }
        finally { await using var release = new NpgsqlCommand($"SELECT pg_advisory_unlock({key})", gate); await release.ExecuteNonQueryAsync(); await f.Fault(space, false); }
    }

    [Fact]
    public async Task History_auth_database_failure_returns_503_and_no_body()
    {
        var space = await f.Open("search07");
        var original = f.D.Inner.Factory.Services.GetService(typeof(IConfiguration)) as IConfiguration;
        var unreachable = new NpgsqlConnectionStringBuilder(original!.GetConnectionString("Database")!) { Host = "127.0.0.1", Port = 1, Timeout = 1, Pooling = false };
        using var host = f.D.Inner.Factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Database"] = unreachable.ConnectionString })));
        for (var i = 0; i < 2; i++) await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), "authority outage fixture");
        var cursor = Cursor(await Page(space, "?limit=1"))!;
        using var client = host.CreateClient(); using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/direct-conversations/{space}/messages" + Before(cursor, 1));
        request.Headers.Authorization = new("Bearer", f.D.Inner.Actor.AccessToken);
        using var response = await client.SendAsync(request); Assert.Equal(503, (int)response.StatusCode);
        Assert.Equal("AUTHORITY_UNAVAILABLE", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Before_pages_keep_frontier_and_current_access_is_rechecked_on_every_request()
    {
        var space = await f.Open("search08"); var peer = f.D.Inner.Users["search08"];
        for (var i = 0; i < 3; i++) await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), "snapshot");
        var first = await Page(space, "?limit=1", peer); var cursor = Cursor(first)!;
        await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), "outside old H");
        var older = await Page(space, Before(cursor, 1), peer);
        Assert.Equal("3", older.GetProperty("throughSequence").GetString()); Assert.Equal("2", older.GetProperty("items")[0].GetProperty("sequence").GetString());
        await f.D.Sql("UPDATE identity.user_emails SET verified_at=NULL WHERE user_id=@id AND is_primary", peer.Id);
        await Page(space, Before(cursor, 1), peer, 401);
        await f.D.Sql("UPDATE identity.user_emails SET verified_at=now() WHERE user_id=@id AND is_primary", peer.Id);
        await f.D.Sql("UPDATE messaging.spaces SET status=3,deleted_at=now() WHERE id=@id", space);
        var denied = await Page(space, Before(cursor, 1), peer, 404);
        Assert.False(denied.TryGetProperty("items", out _)); Assert.False(denied.TryGetProperty("throughSequence", out _));
    }
}
