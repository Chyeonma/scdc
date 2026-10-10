using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;

namespace SCDC.Api.Tests.Messaging;

[Collection("DM database")]
public sealed class SequenceAndKeyTests(TextMessageFixture f) : IClassFixture<TextMessageFixture>
{
    private async Task<JsonElement> Page(Guid space, string query = "")
    {
        using var response = await f.D.Inner.Send($"/api/v1/direct-conversations/{space}/messages{query}");
        Assert.Equal(200, (int)response.StatusCode); return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Theory]
    [InlineData(false, "search10")]
    [InlineData(true, "search11")]
    public async Task Independent_sessions_and_reader_wait_for_writer_commit_or_rollback(bool rollback, string peer)
    {
        var space = await f.Open(peer); var a1 = await f.D.Inner.Login(f.D.Inner.Actor.Username);
        var a2 = await f.D.Inner.Login(f.D.Inner.Actor.Username); Assert.NotEqual(a1.SessionId, a2.SessionId);
        var baseline = await Page(space); var cursor = baseline.GetProperty("resumeCursor").GetString()!;
        var x = Guid.NewGuid(); var y = Guid.NewGuid(); var key = Random.Shared.NextInt64(1_000_000_000, 9_000_000_000);
        await using var gate = await f.D.Connect();
        async Task Unlock() { await using var command = new NpgsqlCommand($"SELECT pg_advisory_unlock({key});", gate); await command.ExecuteNonQueryAsync(); }
        await using (var command = new NpgsqlCommand($"SELECT pg_advisory_lock({key});", gate)) await command.ExecuteNonQueryAsync();
        var action = $"IF EXISTS(SELECT 1 FROM messaging.messages WHERE id=NEW.aggregate_id AND client_message_id='{x}') THEN PERFORM pg_advisory_xact_lock({key}); "
            + (rollback ? "RAISE EXCEPTION USING ERRCODE='58000',MESSAGE='Owned sequence rollback'; " : "") + "END IF;";
        try
        {
            await f.Fault(space, true, action);
            var writerX = f.Send(space, a1, x, "SEQ-X", rollback ? 503 : 200); await f.WaitForLockChain(gate.ProcessID, 1);
            var writerY = f.Send(space, a2, y, "SEQ-Y"); await f.WaitForLockChain(gate.ProcessID, 2);
            var reader = Page(space, "?after=" + Uri.EscapeDataString(cursor)); await f.WaitForLockChain(gate.ProcessID, 3);
            Assert.False(writerX.IsCompleted); Assert.False(writerY.IsCompleted); Assert.False(reader.IsCompleted);
            Assert.Equal("0:0:0:0", await f.Counts(space)); // MVCC probe, not a history read that bypasses the lock.
            await Unlock(); var responses = await Task.WhenAll(writerX, writerY); await reader;
            var final = await Page(space, "?after=" + Uri.EscapeDataString(cursor));
            if (rollback)
            {
                Assert.Equal("AUTHORITY_UNAVAILABLE", responses[0].GetProperty("errorCode").GetString());
                Assert.Equal("1", responses[1].GetProperty("sequence").GetString()); Assert.Single(final.GetProperty("items").EnumerateArray());
                Assert.Equal(y, final.GetProperty("items")[0].GetProperty("clientMessageId").GetGuid()); Assert.Equal("1:1:1:1", await f.Counts(space));
            }
            else
            {
                Assert.Equal(new[] { "1", "2" }, responses.Select(r => r.GetProperty("sequence").GetString()));
                Assert.Equal(responses.Select(r => r.GetProperty("id").GetGuid()), final.GetProperty("items").EnumerateArray().Select(r => r.GetProperty("id").GetGuid()));
                Assert.Equal("2:2:2:2", await f.Counts(space));
            }
        }
        finally { await Unlock(); await f.Fault(space, false); }
    }

    [Fact]
    public async Task Hash_mismatch_at_first_middle_and_last_byte_is_conflict_without_mutation()
    {
        var space = await f.Open("search12"); var operation = Guid.NewGuid();
        await f.Send(space, f.D.Inner.Actor, operation, " a\r\nb ");
        foreach (var index in new[] { 0, 15, 31 })
        {
            var sql = $"UPDATE messaging.send_operations SET fingerprint=set_byte(fingerprint,{index},get_byte(fingerprint,{index}) # 1) WHERE space_id=@id";
            await f.D.Sql(sql, space);
            try { Assert.Equal("OPERATION_CONFLICT", (await f.Send(space, f.D.Inner.Actor, operation, " a\nb ", 409)).GetProperty("errorCode").GetString()); Assert.Equal("1:1:1:1", await f.Counts(space)); }
            finally { await f.D.Sql(sql, space); }
        }
        var replay = await f.Send(space, f.D.Inner.Actor, operation, " a\nb "); Assert.Equal(" a\nb ", replay.GetProperty("content").GetString());
        // Code uses CryptographicOperations.FixedTimeEquals. Positions above prove behavior, not timing guarantees.
    }

    [Fact]
    public async Task Catchup_101_pages_preserve_frontier_and_resume_only_on_final_page()
    {
        var space = await f.Open("search13"); var baseline = await Page(space); var cursor = baseline.GetProperty("resumeCursor").GetString()!;
        var expected = new List<Guid>();
        for (var i = 1; i <= 101; i++) expected.Add((await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), $"R101-{i:000}")).GetProperty("id").GetGuid());
        var actual = new List<Guid>(); var sizes = new List<int>();
        do
        {
            var page = await Page(space, "?after=" + Uri.EscapeDataString(cursor)); sizes.Add(page.GetProperty("items").GetArrayLength());
            Assert.Equal("101", page.GetProperty("throughSequence").GetString());
            actual.AddRange(page.GetProperty("items").EnumerateArray().Select(m => m.GetProperty("id").GetGuid()));
            var next = page.GetProperty("nextCursor").GetString();
            if (next is null) { Assert.NotNull(page.GetProperty("resumeCursor").GetString()); break; }
            Assert.Equal(JsonValueKind.Null, page.GetProperty("resumeCursor").ValueKind); cursor = next;
        } while (true);
        Assert.Equal(new[] { 50, 50, 1 }, sizes); Assert.Equal(expected, actual); Assert.Equal("101:101:101:101", await f.Counts(space));
    }
}
