using System.Net.Http.Json;
using System.Text.Json;
using SCDC.Api.Tests.Identity;

namespace SCDC.Api.Tests.Messaging;

[Collection("DM database")]
public sealed class MissingHistoricalUserTests(TextMessageFixture f) : IClassFixture<TextMessageFixture>
{
    private async Task<JsonElement> ReadAsync(string route, UserSearchFixture.ActorData? actor = null, int status = 200)
    {
        using var response = await f.D.Inner.Send(route, actor);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True((int)response.StatusCode == status, $"Expected {status}, got {(int)response.StatusCode}: {body}");
        return body;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SchemaBoundary_missing_Identity_account_or_profile_keeps_DM_history_but_denies_new_interactions(bool profileOnly)
    {
        var alias = profileOnly ? "search20" : "search21";
        var peer = f.D.Inner.Users[alias];
        var space = await f.Open(alias);
        var operation = Guid.NewGuid();
        var own = await f.Send(space, f.D.Inner.Actor, operation, "own retained body");
        var theirs = await f.Send(space, peer, Guid.NewGuid(), "peer retained body");
        var counts = await f.Counts(space);
        await f.D.Sql(profileOnly ? "DELETE FROM identity.user_profiles WHERE user_id=@id" : "DELETE FROM identity.users WHERE id=@id", peer.Id);
        Assert.Equal(counts, await f.Counts(space));

        var opened = await f.D.Open(f.D.Inner.Actor, peer.Id);
        Assert.Equal(space, opened.GetProperty("id").GetGuid());
        var person = opened.GetProperty("participants").EnumerateArray().Single(p => p.GetProperty("id").GetGuid() == peer.Id);
        Assert.Equal("unavailable", person.GetProperty("availability").GetString());
        Assert.Equal("unavailable", person.GetProperty("username").GetString());
        Assert.Equal("Người dùng không khả dụng", person.GetProperty("displayName").GetString());

        var page = await ReadAsync($"/api/v1/direct-conversations/{space}/messages");
        var message = page.GetProperty("items").EnumerateArray().Single(m => m.GetProperty("id").GetGuid() == theirs.GetProperty("id").GetGuid());
        Assert.Equal("peer retained body", message.GetProperty("content").GetString());
        Assert.Equal("unavailable", message.GetProperty("author").GetProperty("availability").GetString());
        var inbox = await ReadAsync("/api/v1/direct-conversations");
        Assert.Contains(inbox.GetProperty("items").EnumerateArray(), item => item.GetProperty("id").GetGuid() == space
            && item.GetProperty("participants").EnumerateArray().Any(p => p.GetProperty("id").GetGuid() == peer.Id && p.GetProperty("availability").GetString() == "unavailable"));
        Assert.Equal(own.GetProperty("id").GetGuid(), (await f.Send(space, f.D.Inner.Actor, operation, "own retained body")).GetProperty("id").GetGuid());
        Assert.Equal(counts, await f.Counts(space));
        await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), "must not write", 404);
        await f.Send(space, peer, Guid.NewGuid(), "missing actor must not write", 401);
        await ReadAsync($"/api/v1/direct-conversations/{space}/messages", peer, 401);
        await f.D.Open(f.D.Inner.Users["search22"], peer.Id, 404);
        await ReadAsync($"/api/v1/direct-conversations/{space}/messages", f.D.Inner.Users["search22"], 404);

        var id = own.GetProperty("id").GetGuid();
        using var edit = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/direct-conversations/{space}/messages/{id}")
            { Content = JsonContent.Create(new { expectedVersion = "1", content = "edited by author" }) };
        edit.Headers.Authorization = new("Bearer", f.D.Inner.Actor.AccessToken);
        using var edited = await f.D.Inner.Client.SendAsync(edit);
        Assert.Equal(200, (int)edited.StatusCode);
        using var delete = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/direct-conversations/{space}/messages/{id}?expectedVersion=2");
        delete.Headers.Authorization = new("Bearer", f.D.Inner.Actor.AccessToken);
        using var deleted = await f.D.Inner.Client.SendAsync(delete);
        Assert.Equal(200, (int)deleted.StatusCode);
    }
}
