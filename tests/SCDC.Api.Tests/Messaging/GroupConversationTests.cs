using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SCDC.Api.Tests.Messaging;

[Collection("DM database")]
public sealed class GroupConversationTests(TextMessageFixture fixture) : IClassFixture<TextMessageFixture>
{
    private const string Groups = "/api/v1/group-conversations";

    [Fact]
    public async Task Reply_targets_stay_in_the_same_group_and_are_bound_to_the_send_operation()
    {
        var api = fixture.D.Inner;
        var reader = api.Users["bao"];
        var memberIds = new[] { reader.Id, api.Users["chi"].Id };
        var id = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        foreach (var groupId in new[] { id, otherId })
        {
            using var group = await api.Send(Groups, api.Actor, new { id = groupId, name = "Group replies", memberIds });
            Assert.Equal(HttpStatusCode.OK, group.StatusCode);
        }
        var path = $"{Groups}/{id}/messages";
        using var sent = await api.Send(path, api.Actor, new { clientMessageId = Guid.NewGuid(), content = "Parent message" });
        Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        var parent = (await sent.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var command = new { clientMessageId = Guid.NewGuid(), content = "A reply", replyToMessageId = parent };
        using var rejected = await api.Send($"{Groups}/{otherId}/messages", reader, command);
        Assert.Equal(HttpStatusCode.NotFound, rejected.StatusCode);
        using var reply = await api.Send(path, reader, command);
        Assert.Equal(HttpStatusCode.OK, reply.StatusCode);
        var replyBody = await reply.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(parent, replyBody.GetProperty("reply").GetProperty("id").GetGuid());
        Assert.Equal("Parent message", replyBody.GetProperty("reply").GetProperty("content").GetString());
        using var replay = await api.Send(path, reader, command);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(replyBody.GetProperty("id").GetGuid(), (await replay.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());
        using var mismatch = await api.Send(path, reader, new { command.clientMessageId, command.content });
        Assert.Equal(HttpStatusCode.Conflict, mismatch.StatusCode);
        using var history = await api.Send(path, api.Actor);
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        var page = await history.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(parent, page.GetProperty("items")[1].GetProperty("reply").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Group_likes_are_idempotent_private_and_present_in_history()
    {
        var api = fixture.D.Inner;
        var reader = api.Users["bao"];
        var other = api.Users["chi"];
        var id = Guid.NewGuid();
        using var created = await api.Send(Groups, api.Actor, new { id, name = "Group likes", memberIds = new[] { reader.Id, other.Id } });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var messages = $"{Groups}/{id}/messages";
        using var sent = await api.Send(messages, api.Actor, new { clientMessageId = Guid.NewGuid(), content = "Like this" });
        Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        var message = await sent.Content.ReadFromJsonAsync<JsonElement>();
        var route = $"{messages}/{message.GetProperty("id").GetGuid()}/like";

        foreach (var method in new[] { HttpMethod.Put, HttpMethod.Put, HttpMethod.Delete, HttpMethod.Delete })
        {
            using var request = new HttpRequestMessage(method, route);
            request.Headers.Authorization = new("Bearer", reader.AccessToken);
            using var response = await api.Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(method == HttpMethod.Put ? 1 : 0, result.GetProperty("count").GetInt32());
            if (method == HttpMethod.Put)
            {
                using var history = await api.Send(messages, other);
                var page = await history.Content.ReadFromJsonAsync<JsonElement>();
                var likes = page.GetProperty("items")[0].GetProperty("likes");
                Assert.Equal(1, likes.GetProperty("count").GetInt32());
                Assert.False(likes.GetProperty("likedByMe").GetBoolean());
            }
        }
        using var denied = await api.Send(route, api.Users["search01"]);
        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
    }

    [Fact]
    public async Task Group_is_visible_to_members_and_reuses_the_message_pipeline()
    {
        var api = fixture.D.Inner;
        var u = api.Users["bao"];
        var v = api.Users["search01"];
        var outsider = api.Users["search02"];
        var id = Guid.NewGuid();
        var group = new { id, name = "Release chat", memberIds = new[] { u.Id, v.Id } };
        using var created = await api.Send(Groups, api.Actor, group);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        using var replay = await api.Send(Groups, api.Actor, group);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        using var conflict = await api.Send(Groups, api.Actor, new { group.id, name = "Different", group.memberIds });
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);

        foreach (var actor in new[] { u, v, outsider })
        {
            using var list = await api.Send(Groups, actor);
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            using var page = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
            Assert.Equal(actor != outsider, page.RootElement.GetProperty("items").EnumerateArray()
                .Any(item => item.GetProperty("id").GetGuid() == id));
        }

        var path = $"{Groups}/{id}/messages";
        var message = new { clientMessageId = Guid.NewGuid(), content = "Hello group" };
        using var send = await api.Send(path, u, message);
        Assert.Equal(HttpStatusCode.OK, send.StatusCode);
        var sent = await send.Content.ReadFromJsonAsync<JsonElement>();
        using var retriedSend = await api.Send(path, u, message);
        Assert.Equal(HttpStatusCode.OK, retriedSend.StatusCode);
        var retried = await retriedSend.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(sent.GetProperty("id").GetGuid(), retried.GetProperty("id").GetGuid());
        var messagePath = $"{path}/{sent.GetProperty("id").GetGuid()}";
        using var history = await api.Send(path, v);
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        using var body = JsonDocument.Parse(await history.Content.ReadAsStringAsync());
        Assert.Equal("Hello group", Assert.Single(body.RootElement.GetProperty("items").EnumerateArray()).GetProperty("content").GetString());
        using var deniedRead = await api.Send(path, outsider);
        using var deniedSend = await api.Send(path, outsider, message);
        using var wrongRoute = await api.Send($"/api/v1/direct-conversations/{id}/messages", v);
        Assert.Equal(HttpStatusCode.NotFound, deniedRead.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deniedSend.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, wrongRoute.StatusCode);
        using var editRequest = new HttpRequestMessage(HttpMethod.Patch, messagePath)
        {
            Content = JsonContent.Create(new { expectedVersion = "1", content = "Changed" })
        };
        editRequest.Headers.Authorization = new("Bearer", v.AccessToken);
        using var edit = await api.Client.SendAsync(editRequest);
        Assert.Equal(HttpStatusCode.Forbidden, edit.StatusCode);

        using var ownEdit = new HttpRequestMessage(HttpMethod.Patch, messagePath)
        {
            Content = JsonContent.Create(new { expectedVersion = "1", content = "Changed" })
        };
        ownEdit.Headers.Authorization = new("Bearer", u.AccessToken);
        using var updated = await api.Client.SendAsync(ownEdit);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var edited = await updated.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Changed", edited.GetProperty("content").GetString());

        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, messagePath + "?expectedVersion=2");
        deleteRequest.Headers.Authorization = new("Bearer", u.AccessToken);
        using var deleted = await api.Client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        var tombstone = await deleted.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, tombstone.GetProperty("content").ValueKind);
    }

    [Fact]
    public async Task Group_list_cursor_keeps_the_membership_filter_and_returns_the_next_group()
    {
        var api = fixture.D.Inner;
        var memberIds = new[] { api.Users["bao"].Id, api.Users["chi"].Id };
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        foreach (var id in ids)
        {
            using var created = await api.Send(Groups, api.Users["search05"], new { id, name = "Paginated", memberIds });
            Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        }

        using var first = await api.Send(Groups + "?limit=1", api.Users["search05"]);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var page = await first.Content.ReadFromJsonAsync<JsonElement>();
        var firstId = Assert.Single(page.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid();
        var cursor = page.GetProperty("nextCursor").GetGuid();
        using var next = await api.Send(Groups + $"?limit=1&cursor={cursor}", api.Users["search05"]);
        Assert.Equal(HttpStatusCode.OK, next.StatusCode);
        var nextPage = await next.Content.ReadFromJsonAsync<JsonElement>();
        var secondId = Assert.Single(nextPage.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid();
        Assert.Equal(ids.Order(), new[] { firstId, secondId }.Order());
        Assert.Equal(JsonValueKind.Null, nextPage.GetProperty("nextCursor").ValueKind);
    }

    [Fact]
    public async Task Duplicate_and_unavailable_members_are_rejected()
    {
        var api = fixture.D.Inner;
        var peer = api.Users["bao"].Id;
        using var bad = await api.Send(Groups, api.Actor, new { id = Guid.NewGuid(), name = "Group", memberIds = new[] { peer, peer } });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        using var unavailable = await api.Send(Groups, api.Actor, new { id = Guid.NewGuid(), name = "Group", memberIds = new[] { peer, api.Users["disabled"].Id } });
        Assert.Equal(HttpStatusCode.NotFound, unavailable.StatusCode);
    }

    [Fact]
    public async Task Group_creation_with_nine_recipients_uses_the_existing_identity_contract()
    {
        var api = fixture.D.Inner;
        var memberIds = Enumerable.Range(10, 9).Select(i => api.Users[$"search{i:00}"].Id).ToArray();
        using var created = await api.Send(Groups, api.Actor, new { id = Guid.NewGuid(), name = "Ten members", memberIds });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var group = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(10, group.GetProperty("memberCount").GetInt32());
    }
}
