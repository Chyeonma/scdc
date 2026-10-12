using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;
using SCDC.Api.Tests.Identity;

namespace SCDC.Api.Tests.Messaging;

[Collection("DM database")]
public sealed class MessageMutationTests(TextMessageFixture f) : IClassFixture<TextMessageFixture>
{
    private async Task<JsonElement> Change(Guid space, Guid mid, string version, string? content, bool delete = false,
        UserSearchFixture.ActorData? actor = null, int status = 200, bool anonymous = false)
    {
        using var request = new HttpRequestMessage(delete ? HttpMethod.Delete : HttpMethod.Patch,
            $"/api/v1/direct-conversations/{space}/messages/{mid}" + (delete ? "?expectedVersion=" + Uri.EscapeDataString(version) : ""));
        if (!anonymous) request.Headers.Authorization = new("Bearer", (actor ?? f.D.Inner.Actor).AccessToken);
        if (!delete) request.Content = JsonContent.Create(new { content, expectedVersion = version });
        using var response = await f.D.Inner.Client.SendAsync(request); Assert.Equal(status, (int)response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
    [Fact]
    public async Task Multiple_edits_noop_delete_replay_keep_one_operation_and_never_store_previous_bodies()
    {
        var space = await f.Open("bao"); var operation = Guid.NewGuid();
        var original = await f.Send(space, f.D.Inner.Actor, operation, "original-private"); var mid = original.GetProperty("id").GetGuid();
        var edited = await Change(space, mid, "1", "  edited\r\nbody  "); Assert.Equal("2", edited.GetProperty("version").GetString());
        Assert.Equal("  edited\nbody  ", edited.GetProperty("content").GetString());
        var noop = await Change(space, mid, "2", "  edited\r\nbody  "); Assert.Equal(edited.GetRawText(), noop.GetRawText()); Assert.Equal("1:1:2:1", await f.Counts(space));
        var final = await Change(space, mid, "2", "last-current"); Assert.Equal("3", final.GetProperty("version").GetString());
        var deleted = await Change(space, mid, "3", null, true); Assert.Equal(JsonValueKind.Null, deleted.GetProperty("content").ValueKind);
        Assert.Equal("4", deleted.GetProperty("version").GetString()); Assert.Equal("1", deleted.GetProperty("sequence").GetString());
        var repeated = await Change(space, mid, "1", null, true); Assert.Equal(deleted.GetRawText(), repeated.GetRawText());
        Assert.Equal("MESSAGE_DELETED", (await Change(space, mid, "4", "resurrect", status: 409)).GetProperty("errorCode").GetString());
        var replay = await f.Send(space, f.D.Inner.Actor, operation, "original-private"); Assert.Equal(deleted.GetRawText(), replay.GetRawText());
        Assert.Equal("1:1:4:1", await f.Counts(space));
        Assert.Equal("0", await f.Scalar("SELECT count(*)::text FROM messaging.message_edits e JOIN messaging.messages m ON m.id=e.message_id WHERE m.space_id=@id", space));
        Assert.Equal("0", await f.Scalar("SELECT count(*)::text FROM integration.outbox_events WHERE space_id=@id AND (payload ? 'content' OR payload ? 'body' OR payload::text LIKE '%original-private%' OR payload::text LIKE '%edited%' OR payload::text LIKE '%last-current%')", space));
    }
    [Fact]
    public async Task Author_guard_cross_space_outsider_and_disabled_peer_do_not_change_access_purpose()
    {
        var space = await f.Open("search01"); var other = await f.Open("chi"); var op = Guid.NewGuid();
        var mid = (await f.Send(space, f.D.Inner.Actor, op, "guard-original")).GetProperty("id").GetGuid();
        foreach(var delete in new[]{false,true})
        {
            Assert.Equal("AUTHOR_REQUIRED", (await Change(space, mid, "1", "forbidden", delete, f.D.Inner.Users["search01"], 403)).GetProperty("errorCode").GetString());
            Assert.Equal("RESOURCE_NOT_FOUND", (await Change(space, mid, "1", "private", delete, f.D.Inner.Users["chi"], 404)).GetProperty("errorCode").GetString());
            await Change(other, mid, "1", "cross-space", delete, status:404); await Change(space, mid, "1", "anonymous", delete, status:401, anonymous:true);
        }
        await f.D.Sql("UPDATE identity.users SET status=3 WHERE id=@id", f.D.Inner.Users["search01"].Id);
        try
        {
            await Change(space, mid, "1", "peer-disabled-edit"); await Change(space, mid, "2", null, true);
            await f.Send(space, f.D.Inner.Actor, op, "guard-original"); await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), "new-blocked", 404);
            Assert.Equal("1:1:3:1", await f.Counts(space));
        }
        finally { await f.D.Sql("UPDATE identity.users SET status=1 WHERE id=@id", f.D.Inner.Users["search01"].Id); }
    }
    [Fact]
    public async Task Validation_stale_version_and_outbox_failure_do_not_change_current_message()
    {
        var space = await f.Open("search02"); var mid = (await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), "stable")).GetProperty("id").GetGuid();
        Assert.Equal("VERSION_CONFLICT", (await Change(space, mid, "9007199254740993", "stale", status:409)).GetProperty("errorCode").GetString());
        foreach(var version in new[]{"0","01","-1","1.0",""}) await Change(space, mid, version, "invalid", status:400);
        await Change(space, mid, "1", new string('a',2001), status:400);
        try { await f.Fault(space, true); await Change(space, mid, "1", "rolled-back", status:503); }
        finally { await f.Fault(space, false); }
        Assert.Equal("stable", await f.Scalar("SELECT content FROM messaging.messages WHERE space_id=@id", space));
        Assert.Equal("1:1:1:1", await f.Counts(space)); await Change(space, mid, "1", new string('a',2000));
    }
    [Theory]
    [InlineData(false,"search03")]
    [InlineData(true,"search04")]
    public async Task Held_first_mutation_commits_once_second_expected_version_conflicts(bool deletion,string alias)
    {
        var space = await f.Open(alias); var mid = (await f.Send(space,f.D.Inner.Actor,Guid.NewGuid(),"race-original")).GetProperty("id").GetGuid();
        var key=Random.Shared.NextInt64(1000000000,9000000000); await using var gate=await f.D.Connect();
        async Task Unlock(){await using var sql=new NpgsqlCommand($"SELECT pg_advisory_unlock({key})",gate);await sql.ExecuteNonQueryAsync();}
        await using(var sql=new NpgsqlCommand($"SELECT pg_advisory_lock({key})",gate))await sql.ExecuteNonQueryAsync();
        try
        {
            await f.Fault(space,true,$"PERFORM pg_advisory_xact_lock({key});");
            var first=Change(space,mid,"1","race-winner");await f.WaitForLockChain(gate.ProcessID,1);
            var second=Change(space,mid,"1",deletion?null:"race-loser",deletion,status:409);await f.WaitForLockChain(gate.ProcessID,2);
            Assert.False(first.IsCompleted);Assert.False(second.IsCompleted);await Unlock();await first;
            Assert.Equal("VERSION_CONFLICT",(await second).GetProperty("errorCode").GetString());
            Assert.Equal("race-winner",await f.Scalar("SELECT content FROM messaging.messages WHERE space_id=@id",space));Assert.Equal("1:1:2:1",await f.Counts(space));
        }
        finally {await Unlock();await f.Fault(space,false);}
    }
}
