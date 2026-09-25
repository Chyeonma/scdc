using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SCDC.Api.Tests.Infrastructure;

namespace SCDC.Api.Tests.Messaging;

public sealed class UnifiedSpaceMessageFlowTests(SCDCWebApplicationFactory factory)
    : IClassFixture<SCDCWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Mentions_are_scoped_idempotent_and_follow_edits_deletes_and_preferences()
    {
        var actors = new List<TestActor>();
        try
        {
            var author = await CreateActorAsync("mention_author");
            var reader = await CreateActorAsync("mention_reader");
            var outsider = await CreateActorAsync("mention_outsider");
            actors.AddRange([author, reader, outsider]);
            var created = await SendAsync(HttpMethod.Post, "/api/v1/conversations/direct", author.Token,
                new { recipientUserId = reader.Id });
            var spaceId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
            var rootPath = $"/api/v1/spaces/{spaceId}/messages";

            var suggestions = await SendAsync(HttpMethod.Get,
                $"{rootPath}/mentions/suggestions?query={reader.Username[..8]}", author.Token);
            Assert.Equal(HttpStatusCode.OK, suggestions.StatusCode);
            var candidates = await suggestions.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains(candidates.EnumerateArray(), item => item.GetProperty("userId").GetGuid() == reader.Id);
            Assert.DoesNotContain(candidates.EnumerateArray(), item => item.GetProperty("userId").GetGuid() == outsider.Id);
            Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Get,
                $"{rootPath}/mentions/suggestions?query={reader.Username[..8]}", outsider.Token)).StatusCode);

            var preferencesPath = $"/api/v1/spaces/{spaceId}/preferences";
            async Task SetPreferencesAsync(short level, DateTimeOffset? mutedUntil = null)
            {
                var response = await SendAsync(HttpMethod.Put, preferencesPath, reader.Token,
                    new { notificationLevel = level, mutedUntil, isHidden = false, isPinned = false });
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
            async Task<int> NotificationCountAsync()
            {
                var response = await SendAsync(HttpMethod.Get, "/api/v1/spaces", reader.Token);
                var body = await response.Content.ReadFromJsonAsync<JsonElement>();
                return body.GetProperty("items").EnumerateArray()
                    .Single(item => item.GetProperty("id").GetGuid() == spaceId)
                    .GetProperty("notificationCount").GetInt32();
            }

            await SetPreferencesAsync(1);
            var clientMessageId = Guid.NewGuid();
            var content = $"hello @{reader.Username} @{reader.Username} @{outsider.Username} @everyone";
            var sent = await SendAsync(HttpMethod.Post, rootPath, author.Token,
                new { clientMessageId, messageType = 1, content });
            Assert.Equal(HttpStatusCode.Created, sent.StatusCode);
            var message = await sent.Content.ReadFromJsonAsync<JsonElement>();
            var messageId = message.GetProperty("id").GetGuid();
            Assert.Equal(reader.Id, message.GetProperty("mentions").EnumerateArray().Single().GetProperty("userId").GetGuid());
            Assert.Equal(1, await NotificationCountAsync());

            var retry = await SendAsync(HttpMethod.Post, rootPath, author.Token,
                new { clientMessageId, messageType = 1, content });
            Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
            Assert.Single((await retry.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("mentions").EnumerateArray());
            Assert.Equal(1, await NotificationCountAsync());

            var path = $"{rootPath}/{messageId}";
            var edited = await SendAsync(HttpMethod.Patch, path, author.Token,
                new { content = $"removed @{outsider.Username}", expectedVersion = 1 });
            Assert.Equal(HttpStatusCode.OK, edited.StatusCode);
            Assert.Empty((await edited.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("mentions").EnumerateArray());
            Assert.Equal(0, await NotificationCountAsync());
            edited = await SendAsync(HttpMethod.Patch, path, author.Token,
                new { content = $"again @{reader.Username}", expectedVersion = 2 });
            Assert.Equal(HttpStatusCode.OK, edited.StatusCode);
            Assert.Equal(1, await NotificationCountAsync());

            await SetPreferencesAsync(1, DateTimeOffset.UtcNow.AddHours(1));
            Assert.Equal(0, await NotificationCountAsync());
            await SetPreferencesAsync(0);
            Assert.Equal(0, await NotificationCountAsync());
            await SetPreferencesAsync(1);
            Assert.Equal(1, await NotificationCountAsync());
            Assert.Equal(HttpStatusCode.NoContent,
                (await SendAsync(HttpMethod.Delete, $"{path}?expectedVersion=3", author.Token)).StatusCode);
            Assert.Equal(0, await NotificationCountAsync());
            var deleted = await SendAsync(HttpMethod.Get, path, reader.Token);
            Assert.Empty((await deleted.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("mentions").EnumerateArray());

            var root = await SendTextAsync(author, spaceId);
            var rootId = root.GetProperty("id").GetGuid();
            var read = await SendAsync(HttpMethod.Put, $"/api/v1/spaces/{spaceId}/read-state", reader.Token,
                new { lastReadSequence = root.GetProperty("sequenceNo").GetString() });
            Assert.Equal(HttpStatusCode.OK, read.StatusCode);
            var thread = await SendAsync(HttpMethod.Post, rootPath, author.Token,
                new { clientMessageId = Guid.NewGuid(), messageType = 1,
                    content = $"thread @{reader.Username}", replyToMessageId = rootId, threadRootId = rootId });
            Assert.Equal(HttpStatusCode.Created, thread.StatusCode);
            Assert.Equal(1, await NotificationCountAsync());
            var inbox = await SendAsync(HttpMethod.Get, "/api/v1/spaces", reader.Token);
            var summary = (await inbox.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items")
                .EnumerateArray().Single(item => item.GetProperty("id").GetGuid() == spaceId);
            Assert.Equal(0, summary.GetProperty("unreadCount").GetInt32());
            await SetPreferencesAsync(2);
            Assert.Equal(1, await NotificationCountAsync());
        }
        finally
        {
            await CleanupAsync(actors);
        }
    }

    [Fact]
    public async Task Replies_and_threads_persist_with_one_level_roots_cursor_and_tombstones()
    {
        var actors = new List<TestActor>();
        try
        {
            var author = await CreateActorAsync("thread_author");
            var reader = await CreateActorAsync("thread_reader");
            var outsider = await CreateActorAsync("thread_outsider");
            actors.AddRange([author, reader, outsider]);
            var dm = await SendAsync(HttpMethod.Post, "/api/v1/conversations/direct", author.Token,
                new { recipientUserId = reader.Id });
            var spaceId = (await dm.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
            var otherDm = await SendAsync(HttpMethod.Post, "/api/v1/conversations/direct", author.Token,
                new { recipientUserId = outsider.Id });
            var otherSpaceId = (await otherDm.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
            var root = await SendTextAsync(author, spaceId);
            var rootId = root.GetProperty("id").GetGuid();
            var otherRoot = await SendTextAsync(author, otherSpaceId);
            var otherRootId = otherRoot.GetProperty("id").GetGuid();

            var crossSpace = await SendAsync(HttpMethod.Post, $"/api/v1/spaces/{spaceId}/messages", author.Token,
                new { clientMessageId = Guid.NewGuid(), messageType = 1, content = "cross", replyToMessageId = otherRootId });
            Assert.Equal(HttpStatusCode.NotFound, crossSpace.StatusCode);

            var plain = await SendAsync(HttpMethod.Post, $"/api/v1/spaces/{spaceId}/messages", author.Token,
                new { clientMessageId = Guid.NewGuid(), messageType = 1, content = "plain reply", replyToMessageId = rootId });
            Assert.Equal(HttpStatusCode.Created, plain.StatusCode);
            var plainDto = await plain.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(rootId, plainDto.GetProperty("replyToMessageId").GetGuid());
            Assert.Equal(JsonValueKind.Null, plainDto.GetProperty("threadRootId").ValueKind);

            var threadClientId = Guid.NewGuid();
            var first = await SendAsync(HttpMethod.Post, $"/api/v1/spaces/{spaceId}/messages", author.Token,
                new { clientMessageId = threadClientId, messageType = 1, content = "thread one", replyToMessageId = rootId, threadRootId = rootId });
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            var firstDto = await first.Content.ReadFromJsonAsync<JsonElement>();
            var firstId = firstDto.GetProperty("id").GetGuid();
            Assert.Equal(rootId, firstDto.GetProperty("threadRootId").GetGuid());

            var nested = await SendAsync(HttpMethod.Post, $"/api/v1/spaces/{spaceId}/messages", author.Token,
                new { clientMessageId = Guid.NewGuid(), messageType = 1, content = "thread two", replyToMessageId = firstId, threadRootId = rootId });
            Assert.Equal(HttpStatusCode.Created, nested.StatusCode);
            var nestedDto = await nested.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(firstId, nestedDto.GetProperty("replyToMessageId").GetGuid());
            Assert.Equal(rootId, nestedDto.GetProperty("threadRootId").GetGuid());
            var history = await SendAsync(HttpMethod.Get, $"/api/v1/spaces/{spaceId}/messages?limit=20", reader.Token);
            var historyItems = (await history.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray().ToArray();
            Assert.Contains(historyItems, item => item.GetProperty("id").GetGuid() == firstId
                && item.GetProperty("threadRootId").GetGuid() == rootId);

            var invalidNested = await SendAsync(HttpMethod.Post, $"/api/v1/spaces/{spaceId}/messages", author.Token,
                new { clientMessageId = Guid.NewGuid(), messageType = 1, content = "invalid", replyToMessageId = firstId });
            Assert.Equal(HttpStatusCode.BadRequest, invalidNested.StatusCode);
            var mixed = await SendAsync(HttpMethod.Post, $"/api/v1/spaces/{spaceId}/messages", author.Token,
                new { clientMessageId = Guid.NewGuid(), messageType = 1, content = "mixed", replyToMessageId = otherRootId, threadRootId = rootId });
            Assert.Equal(HttpStatusCode.NotFound, mixed.StatusCode);

            var rootResponse = await SendAsync(HttpMethod.Get, $"/api/v1/spaces/{spaceId}/messages/{rootId}", reader.Token);
            Assert.Equal(2, (await rootResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("threadCount").GetInt32());
            var pagePath = $"/api/v1/spaces/{spaceId}/messages/{rootId}/replies";
            var latest = await SendAsync(HttpMethod.Get, $"{pagePath}?limit=1", reader.Token);
            var latestPage = await latest.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(latestPage.GetProperty("hasMore").GetBoolean());
            Assert.Single(latestPage.GetProperty("items").EnumerateArray());
            var before = latestPage.GetProperty("nextBeforeSequence").GetString();
            var older = await SendAsync(HttpMethod.Get, $"{pagePath}?limit=1&beforeSequence={before}", reader.Token);
            var olderItem = (await older.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray().Single();
            Assert.Equal(firstId, olderItem.GetProperty("id").GetGuid());
            var forward = await SendAsync(HttpMethod.Get,
                $"{pagePath}?limit=1&afterSequence={firstDto.GetProperty("sequenceNo").GetString()}", reader.Token);
            Assert.Equal(nestedDto.GetProperty("id").GetGuid(),
                (await forward.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray().Single().GetProperty("id").GetGuid());
            Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Get, pagePath, outsider.Token)).StatusCode);

            var inbox = await SendAsync(HttpMethod.Get, "/api/v1/spaces", reader.Token);
            var summary = (await inbox.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items")
                .EnumerateArray().Single(item => item.GetProperty("id").GetGuid() == spaceId);
            Assert.Equal(plainDto.GetProperty("sequenceNo").GetString(), summary.GetProperty("lastMessageSequence").GetString());
            Assert.Equal(2, summary.GetProperty("unreadCount").GetInt32());

            await using (var connection = CreateHubConnection(reader.Token))
            {
                await connection.StartAsync();
                var subscribed = await connection.InvokeAsync<JsonElement>("SubscribeSpace", spaceId);
                Assert.True(subscribed.GetProperty("ok").GetBoolean());
                Assert.Equal(nestedDto.GetProperty("sequenceNo").GetString(),
                    subscribed.GetProperty("value").GetProperty("highWatermark").GetString());
            }

            Assert.Equal(HttpStatusCode.NoContent,
                (await SendAsync(HttpMethod.Delete, $"/api/v1/spaces/{spaceId}/messages/{rootId}?expectedVersion=1", author.Token)).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await SendAsync(HttpMethod.Post, $"/api/v1/spaces/{spaceId}/messages", author.Token,
                new { clientMessageId = Guid.NewGuid(), messageType = 1, content = "late", threadRootId = rootId })).StatusCode);
            var retry = await SendAsync(HttpMethod.Post, $"/api/v1/spaces/{spaceId}/messages", author.Token,
                new { clientMessageId = threadClientId, messageType = 1, content = "thread one", replyToMessageId = rootId, threadRootId = rootId });
            Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
            Assert.Equal(firstId, (await retry.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());
            var afterDelete = await SendAsync(HttpMethod.Get, pagePath, reader.Token);
            Assert.Equal(2, (await afterDelete.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").GetArrayLength());
            Assert.Equal(HttpStatusCode.NoContent,
                (await SendAsync(HttpMethod.Delete, $"/api/v1/spaces/{spaceId}/messages/{firstId}?expectedVersion=1", author.Token)).StatusCode);
            var remainingRoot = await SendAsync(HttpMethod.Get, $"/api/v1/spaces/{spaceId}/messages/{rootId}", reader.Token);
            Assert.Equal(1, (await remainingRoot.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("threadCount").GetInt32());
            var tombstonePage = await SendAsync(HttpMethod.Get, pagePath, reader.Token);
            Assert.Contains((await tombstonePage.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray(),
                item => item.GetProperty("id").GetGuid() == firstId
                    && item.GetProperty("content").ValueKind == JsonValueKind.Null);
        }
        finally
        {
            await CleanupAsync(actors);
        }
    }

    [Fact]
    public async Task Edit_and_delete_enforce_author_version_and_tombstone()
    {
        var actors = new List<TestActor>();
        try
        {
            var author = await CreateActorAsync("edit_author");
            var peer = await CreateActorAsync("edit_peer");
            var third = await CreateActorAsync("edit_third");
            actors.AddRange([author, peer, third]);
            var conversation = await SendAsync(HttpMethod.Post, "/api/v1/conversations/direct", author.Token,
                new { recipientUserId = peer.Id });
            var spaceId = (await conversation.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
            var sent = await SendTextAsync(author, spaceId);
            var messageId = sent.GetProperty("id").GetGuid();
            var path = $"/api/v1/spaces/{spaceId}/messages/{messageId}";

            Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Patch, path, peer.Token,
                new { content = "changed", expectedVersion = 1 })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Delete, $"{path}?expectedVersion=1", peer.Token)).StatusCode);
            var edited = await SendAsync(HttpMethod.Patch, path, author.Token,
                new { content = "updated", expectedVersion = 1 });
            Assert.Equal(HttpStatusCode.OK, edited.StatusCode);
            Assert.Equal(2, (await edited.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetInt32());
            var unchanged = await SendAsync(HttpMethod.Patch, path, author.Token,
                new { content = " updated ", expectedVersion = 2 });
            Assert.Equal(HttpStatusCode.OK, unchanged.StatusCode);
            Assert.Equal(2, (await unchanged.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetInt32());
            Assert.Equal(HttpStatusCode.Conflict, (await SendAsync(HttpMethod.Patch, path, author.Token,
                new { content = "stale", expectedVersion = 1 })).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await SendAsync(HttpMethod.Delete, $"{path}?expectedVersion=1", author.Token)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(HttpMethod.Delete, $"{path}?expectedVersion=2", author.Token)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(HttpMethod.Delete, $"{path}?expectedVersion=1", author.Token)).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await SendAsync(HttpMethod.Patch, path, author.Token,
                new { content = "restore", expectedVersion = 3 })).StatusCode);
            var tombstone = await SendAsync(HttpMethod.Get, path, peer.Token);
            Assert.Equal(HttpStatusCode.OK, tombstone.StatusCode);
            var dto = await tombstone.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(3, dto.GetProperty("version").GetInt32());
            Assert.Equal(JsonValueKind.Null, dto.GetProperty("content").ValueKind);
            Assert.NotEqual(JsonValueKind.Null, dto.GetProperty("deletedAt").ValueKind);

            var connectionString = factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("Database")!;
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("""
                SELECT (SELECT count(*) FROM messaging.message_edits WHERE message_id = @id),
                       (SELECT content FROM messaging.messages WHERE id = @id),
                       (SELECT count(*) FROM integration.outbox_events WHERE aggregate_id = @id AND event_type IN ('Messaging.MessageUpdated','Messaging.MessageDeleted'))
                """, connection);
            command.Parameters.AddWithValue("id", messageId);
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal(1, reader.GetInt64(0));
            Assert.Equal("[deleted]", reader.GetString(1));
            Assert.Equal(2, reader.GetInt64(2));
            await reader.CloseAsync();

            var group = await SendAsync(HttpMethod.Post, "/api/v1/conversations/group", author.Token,
                new { name = "Edit delete rights", memberUserIds = new[] { peer.Id, third.Id } });
            Assert.Equal(HttpStatusCode.Created, group.StatusCode);
            var groupId = (await group.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("spaceId").GetGuid();
            var groupMessage = await SendTextAsync(peer, groupId);
            var groupMessageId = groupMessage.GetProperty("id").GetGuid();
            var groupPath = $"/api/v1/spaces/{groupId}/messages/{groupMessageId}";
            Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Patch, groupPath, author.Token,
                new { content = "not mine", expectedVersion = 1 })).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent,
                (await SendAsync(HttpMethod.Delete, $"{groupPath}?expectedVersion=1", author.Token)).StatusCode);
        }
        finally
        {
            await CleanupAsync(actors);
        }
    }

    [Fact]
    public async Task Dm_group_and_channels_share_message_flow_and_enforce_current_rights()
    {
        var actors = new List<TestActor>();
        try
        {
            var owner = await CreateActorAsync("owner");
            var member = await CreateActorAsync("member");
            var third = await CreateActorAsync("third");
            var outsider = await CreateActorAsync("outsider");
            actors.AddRange([owner, member, third, outsider]);

            var direct = await SendAsync(HttpMethod.Post, "/api/v1/conversations/direct", owner.Token,
                new { recipientUserId = member.Id });
            Assert.Equal(HttpStatusCode.Created, direct.StatusCode);
            var directId = (await direct.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

            var group = await SendAsync(HttpMethod.Post, "/api/v1/conversations/group", owner.Token,
                new { name = "Unified space test", memberUserIds = new[] { member.Id, third.Id } });
            Assert.True(group.StatusCode == HttpStatusCode.Created, await group.Content.ReadAsStringAsync());
            var groupId = (await group.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("spaceId").GetGuid();

            var server = await SendAsync(HttpMethod.Post, "/api/v1/servers", owner.Token,
                new { name = "Unified Space Server" });
            Assert.True(server.StatusCode == HttpStatusCode.Created, await server.Content.ReadAsStringAsync());
            var serverId = (await server.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
            var invite = await SendAsync(HttpMethod.Post, $"/api/v1/servers/{serverId}/invites", owner.Token, new { });
            Assert.Equal(HttpStatusCode.Created, invite.StatusCode);
            var code = (await invite.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();
            var join = await SendAsync(HttpMethod.Post, $"/api/v1/invites/{code}/join", member.Token, new { });
            Assert.Equal(HttpStatusCode.OK, join.StatusCode);

            var publicId = await CreateChannelAsync(owner, serverId, "public-chat", 1);
            var readOnlyId = await CreateChannelAsync(owner, serverId, "announcements", 3);
            var privateId = await CreateChannelAsync(owner, serverId, "private-chat", 2);

            foreach (var spaceId in new[] { directId, groupId, publicId })
            {
                var message = await SendTextAsync(member, spaceId);
                Assert.Equal(spaceId, message.GetProperty("spaceId").GetGuid());
                Assert.Equal(member.Id, message.GetProperty("author").GetProperty("id").GetGuid());
                var history = await SendAsync(HttpMethod.Get, $"/api/v1/spaces/{spaceId}/messages?limit=20", member.Token);
                Assert.Equal(HttpStatusCode.OK, history.StatusCode);
                Assert.Single((await history.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray());
                Assert.Equal(1, await CountMessageOutboxAsync(spaceId));
                Assert.Equal(HttpStatusCode.NotFound,
                    (await SendAsync(HttpMethod.Get, $"/api/v1/spaces/{spaceId}/messages?limit=20", outsider.Token)).StatusCode);
                Assert.Equal(HttpStatusCode.NotFound,
                    (await SendAsync(HttpMethod.Post, $"/api/v1/spaces/{spaceId}/messages", outsider.Token,
                        new { clientMessageId = Guid.NewGuid(), messageType = 1, content = "denied" })).StatusCode);
            }

            await using (var connection = CreateHubConnection(member.Token))
            {
                await connection.StartAsync();
                foreach (var spaceId in new[] { directId, groupId, publicId, readOnlyId })
                {
                    var subscription = await connection.InvokeAsync<JsonElement>("SubscribeSpace", spaceId);
                    Assert.True(subscription.GetProperty("ok").GetBoolean());
                }
                var typingDenied = await connection.InvokeAsync<JsonElement>("SetTyping", readOnlyId, true);
                Assert.False(typingDenied.GetProperty("ok").GetBoolean());
                var denied = await connection.InvokeAsync<JsonElement>("SubscribeSpace", privateId);
                Assert.False(denied.GetProperty("ok").GetBoolean());
            }

            Assert.Equal(HttpStatusCode.OK,
                (await SendAsync(HttpMethod.Get, $"/api/v1/spaces/{readOnlyId}/messages?limit=20", member.Token)).StatusCode);
            var hideChannel = await SendAsync(HttpMethod.Put, $"/api/v1/spaces/{readOnlyId}/preferences", member.Token,
                new { notificationLevel = 2, mutedUntil = (DateTimeOffset?)null, isHidden = true, isPinned = false });
            Assert.Equal(HttpStatusCode.OK, hideChannel.StatusCode);
            var visibleChannels = await SendAsync(HttpMethod.Get, $"/api/v1/servers/{serverId}/channels", member.Token);
            Assert.DoesNotContain((await visibleChannels.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray(),
                item => item.GetProperty("spaceId").GetGuid() == readOnlyId);
            var allChannels = await SendAsync(HttpMethod.Get, $"/api/v1/servers/{serverId}/channels?includeHidden=true", member.Token);
            Assert.Contains((await allChannels.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray(),
                item => item.GetProperty("spaceId").GetGuid() == readOnlyId);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await SendAsync(HttpMethod.Post, $"/api/v1/spaces/{readOnlyId}/messages", member.Token,
                    new { clientMessageId = Guid.NewGuid(), messageType = 1, content = "denied" })).StatusCode);
            await SendTextAsync(owner, readOnlyId);
            visibleChannels = await SendAsync(HttpMethod.Get, $"/api/v1/servers/{serverId}/channels", member.Token);
            Assert.Contains((await visibleChannels.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray(),
                item => item.GetProperty("spaceId").GetGuid() == readOnlyId);
            Assert.Equal(HttpStatusCode.NotFound,
                (await SendAsync(HttpMethod.Get, $"/api/v1/spaces/{privateId}/messages?limit=20", member.Token)).StatusCode);

            await SetUserOverrideAsync(owner, serverId, privateId, member.Id, "channel.read");
            Assert.Equal(HttpStatusCode.OK,
                (await SendAsync(HttpMethod.Get, $"/api/v1/spaces/{privateId}/messages?limit=20", member.Token)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await SendAsync(HttpMethod.Post, $"/api/v1/spaces/{privateId}/messages", member.Token,
                    new { clientMessageId = Guid.NewGuid(), messageType = 1, content = "read only" })).StatusCode);
            await SetUserOverrideAsync(owner, serverId, privateId, member.Id, "channel.send");
            await SendTextAsync(member, privateId);
            Assert.Equal(1, await CountMessageOutboxAsync(privateId));

            var archive = await SendAsync(HttpMethod.Post, $"/api/v1/servers/{serverId}/channels/{publicId}/archive", owner.Token, new { });
            Assert.Equal(HttpStatusCode.NoContent, archive.StatusCode);
            var channels = await SendAsync(HttpMethod.Get, $"/api/v1/servers/{serverId}/channels", member.Token);
            var archived = (await channels.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray()
                .Single(item => item.GetProperty("spaceId").GetGuid() == publicId);
            Assert.Equal(2, archived.GetProperty("status").GetInt32());
            Assert.False(archived.GetProperty("canSend").GetBoolean());
            Assert.Equal(HttpStatusCode.OK,
                (await SendAsync(HttpMethod.Get, $"/api/v1/spaces/{publicId}/messages?limit=20", member.Token)).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict,
                (await SendAsync(HttpMethod.Post, $"/api/v1/spaces/{publicId}/messages", member.Token,
                    new { clientMessageId = Guid.NewGuid(), messageType = 1, content = "archived" })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await SendAsync(HttpMethod.Post, $"/api/v1/spaces/{publicId}/messages", outsider.Token,
                    new { clientMessageId = Guid.NewGuid(), messageType = 1, content = "hidden" })).StatusCode);

            var remove = await SendAsync(HttpMethod.Delete, $"/api/v1/conversations/group/{groupId}/members/{member.Id}", owner.Token);
            Assert.Equal(HttpStatusCode.NoContent, remove.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await SendAsync(HttpMethod.Get, $"/api/v1/spaces/{groupId}/messages?limit=20", member.Token)).StatusCode);
            var kick = await SendAsync(HttpMethod.Post, $"/api/v1/servers/{serverId}/members/{member.Id}/kick", owner.Token, new { });
            Assert.Equal(HttpStatusCode.NoContent, kick.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await SendAsync(HttpMethod.Get, $"/api/v1/spaces/{privateId}/messages?limit=20", member.Token)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await SendAsync(HttpMethod.Post, $"/api/v1/spaces/{privateId}/messages", member.Token,
                    new { clientMessageId = Guid.NewGuid(), messageType = 1, content = "removed" })).StatusCode);
            await SendTextAsync(member, directId); // server/group changes never revoke DM membership
        }
        finally
        {
            await CleanupAsync(actors);
        }
    }

    [Fact]
    public async Task Read_state_is_monotonic_and_unread_counts_actual_messages_across_spaces()
    {
        var actors = new List<TestActor>();
        try
        {
            var owner = await CreateActorAsync("read_owner");
            var reader = await CreateActorAsync("read_reader");
            var third = await CreateActorAsync("read_third");
            actors.AddRange([owner, reader, third]);

            var directResponse = await SendAsync(HttpMethod.Post, "/api/v1/conversations/direct", owner.Token,
                new { recipientUserId = reader.Id });
            Assert.Equal(HttpStatusCode.Created, directResponse.StatusCode);
            var directId = (await directResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
            var groupResponse = await SendAsync(HttpMethod.Post, "/api/v1/conversations/group", owner.Token,
                new { name = "Read state test", memberUserIds = new[] { reader.Id, third.Id } });
            Assert.Equal(HttpStatusCode.Created, groupResponse.StatusCode);
            var groupId = (await groupResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("spaceId").GetGuid();

            var first = await SendTextAsync(owner, directId);
            var groupMessage = await SendTextAsync(owner, groupId);
            var second = await SendTextAsync(owner, directId);
            var firstSequence = first.GetProperty("sequenceNo").GetString()!;
            var secondSequence = second.GetProperty("sequenceNo").GetString()!;
            var groupSequence = groupMessage.GetProperty("sequenceNo").GetString()!;
            Assert.True(long.Parse(secondSequence) - long.Parse(firstSequence) > 1);
            await InsertExcludedMessagesAsync(directId, owner.Id, first.GetProperty("id").GetGuid());

            var inbox = await SendAsync(HttpMethod.Get, "/api/v1/spaces", reader.Token);
            var direct = (await inbox.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray()
                .Single(item => item.GetProperty("id").GetGuid() == directId);
            Assert.Equal(2, direct.GetProperty("unreadCount").GetInt32());
            var ownerInbox = await SendAsync(HttpMethod.Get, "/api/v1/spaces", owner.Token);
            Assert.Equal(0, (await ownerInbox.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray()
                .Single(item => item.GetProperty("id").GetGuid() == directId).GetProperty("unreadCount").GetInt32());
            var groups = await SendAsync(HttpMethod.Get, "/api/v1/conversations/group", reader.Token);
            Assert.Equal(1, (await groups.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray()
                .Single(item => item.GetProperty("spaceId").GetGuid() == groupId).GetProperty("unreadCount").GetInt32());

            Assert.Equal(HttpStatusCode.BadRequest,
                (await SendAsync(HttpMethod.Put, $"/api/v1/spaces/{directId}/read-state", reader.Token,
                    new { lastReadSequence = groupSequence })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,
                (await SendAsync(HttpMethod.Put, $"/api/v1/spaces/{directId}/read-state", reader.Token,
                    new { lastReadSequence = "01" })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await SendAsync(HttpMethod.Put, $"/api/v1/spaces/{directId}/read-state", third.Token,
                    new { lastReadSequence = secondSequence })).StatusCode);

            await using (var otherDevice = CreateHubConnection(reader.Token))
            {
                var updated = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
                otherDevice.On<JsonElement>("RealtimeEvent", envelope =>
                {
                    if (envelope.GetProperty("eventType").GetString() == "SpaceUpdated"
                        && envelope.GetProperty("spaceId").GetGuid() == directId)
                        updated.TrySetResult(envelope);
                });
                await otherDevice.StartAsync();
                var read = await SendAsync(HttpMethod.Put, $"/api/v1/spaces/{directId}/read-state", reader.Token,
                    new { lastReadSequence = secondSequence });
                Assert.Equal(HttpStatusCode.OK, read.StatusCode);
                Assert.Equal(secondSequence, (await read.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("lastReadSequence").GetString());
                await updated.Task.WaitAsync(TimeSpan.FromSeconds(5));
            }

            var stale = await SendAsync(HttpMethod.Put, $"/api/v1/spaces/{directId}/read-state", reader.Token,
                new { lastReadSequence = firstSequence });
            Assert.Equal(HttpStatusCode.OK, stale.StatusCode);
            Assert.Equal(secondSequence, (await stale.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("lastReadSequence").GetString());
            inbox = await SendAsync(HttpMethod.Get, "/api/v1/spaces", reader.Token);
            direct = (await inbox.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray()
                .Single(item => item.GetProperty("id").GetGuid() == directId);
            Assert.Equal(0, direct.GetProperty("unreadCount").GetInt32());

            var serverResponse = await SendAsync(HttpMethod.Post, "/api/v1/servers", owner.Token,
                new { name = "Read state server" });
            Assert.Equal(HttpStatusCode.Created, serverResponse.StatusCode);
            var serverId = (await serverResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
            var invite = await SendAsync(HttpMethod.Post, $"/api/v1/servers/{serverId}/invites", owner.Token, new { });
            var code = (await invite.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();
            Assert.Equal(HttpStatusCode.OK,
                (await SendAsync(HttpMethod.Post, $"/api/v1/invites/{code}/join", reader.Token, new { })).StatusCode);
            var channelId = await CreateChannelAsync(owner, serverId, "read-state", 1);
            var channelMessage = await SendTextAsync(owner, channelId);
            var channelSequence = channelMessage.GetProperty("sequenceNo").GetString()!;
            var channels = await SendAsync(HttpMethod.Get, $"/api/v1/servers/{serverId}/channels", reader.Token);
            Assert.Equal(1, (await channels.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray()
                .Single(item => item.GetProperty("spaceId").GetGuid() == channelId).GetProperty("unreadCount").GetInt32());
            Assert.Equal(HttpStatusCode.OK,
                (await SendAsync(HttpMethod.Put, $"/api/v1/spaces/{channelId}/read-state", reader.Token,
                    new { lastReadSequence = channelSequence })).StatusCode);
            channels = await SendAsync(HttpMethod.Get, $"/api/v1/servers/{serverId}/channels", reader.Token);
            Assert.Equal(0, (await channels.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray()
                .Single(item => item.GetProperty("spaceId").GetGuid() == channelId).GetProperty("unreadCount").GetInt32());
        }
        finally
        {
            await CleanupAsync(actors);
        }
    }

    [Fact]
    public async Task Preferences_survive_refresh_and_typing_respects_subscription_and_send_rights()
    {
        var actors = new List<TestActor>();
        try
        {
            var sender = await CreateActorAsync("pref_sender");
            var reader = await CreateActorAsync("pref_reader");
            var outsider = await CreateActorAsync("pref_outsider");
            actors.AddRange([sender, reader, outsider]);
            var created = await SendAsync(HttpMethod.Post, "/api/v1/conversations/direct", sender.Token,
                new { recipientUserId = reader.Id });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var spaceId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

            var muteUntil = DateTimeOffset.UtcNow.AddHours(1);
            var preferences = await SendAsync(HttpMethod.Put, $"/api/v1/spaces/{spaceId}/preferences", reader.Token,
                new { notificationLevel = 2, mutedUntil = muteUntil, isHidden = true, isPinned = true });
            Assert.Equal(HttpStatusCode.OK, preferences.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,
                (await SendAsync(HttpMethod.Put, $"/api/v1/spaces/{spaceId}/preferences", reader.Token,
                    new { notificationLevel = 7, mutedUntil = (DateTimeOffset?)null, isHidden = false, isPinned = false })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,
                (await SendAsync(HttpMethod.Put, $"/api/v1/spaces/{spaceId}/preferences", reader.Token,
                    new { isHidden = false })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await SendAsync(HttpMethod.Get, $"/api/v1/spaces/{spaceId}/preferences", outsider.Token)).StatusCode);
            var restored = await SendAsync(HttpMethod.Get, $"/api/v1/spaces/{spaceId}/preferences", reader.Token);
            var restoredPreferences = await restored.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(restoredPreferences.GetProperty("isHidden").GetBoolean());
            Assert.True(restoredPreferences.GetProperty("isPinned").GetBoolean());

            var hiddenInbox = await SendAsync(HttpMethod.Get, "/api/v1/spaces", reader.Token);
            Assert.Empty((await hiddenInbox.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray());
            var allInbox = await SendAsync(HttpMethod.Get, "/api/v1/spaces?includeHidden=true", reader.Token);
            Assert.Single((await allInbox.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray());

            var groupResponse = await SendAsync(HttpMethod.Post, "/api/v1/conversations/group", sender.Token,
                new { name = "Preference group", memberUserIds = new[] { reader.Id, outsider.Id } });
            Assert.Equal(HttpStatusCode.Created, groupResponse.StatusCode);
            var groupId = (await groupResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("spaceId").GetGuid();
            Assert.Equal(HttpStatusCode.OK,
                (await SendAsync(HttpMethod.Put, $"/api/v1/spaces/{groupId}/preferences", reader.Token,
                    new { notificationLevel = 0, mutedUntil = (DateTimeOffset?)null, isHidden = true, isPinned = true })).StatusCode);
            var hiddenGroups = await SendAsync(HttpMethod.Get, "/api/v1/conversations/group", reader.Token);
            Assert.Empty((await hiddenGroups.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());
            var allGroups = await SendAsync(HttpMethod.Get, "/api/v1/conversations/group?includeHidden=true", reader.Token);
            Assert.Single((await allGroups.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());

            await using (var senderConnection = CreateHubConnection(sender.Token))
            await using (var readerConnection = CreateHubConnection(reader.Token))
            await using (var outsiderConnection = CreateHubConnection(outsider.Token))
            {
                var started = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
                var stopped = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
                readerConnection.On<JsonElement>("RealtimeEvent", envelope =>
                {
                    if (envelope.GetProperty("eventType").GetString() != "TypingChanged") return;
                    if (envelope.GetProperty("payload").GetProperty("isTyping").GetBoolean()) started.TrySetResult(envelope);
                    else stopped.TrySetResult(envelope);
                });
                await senderConnection.StartAsync();
                await readerConnection.StartAsync();
                await outsiderConnection.StartAsync();
                Assert.True((await senderConnection.InvokeAsync<JsonElement>("SubscribeSpace", spaceId)).GetProperty("ok").GetBoolean());
                Assert.True((await readerConnection.InvokeAsync<JsonElement>("SubscribeSpace", spaceId)).GetProperty("ok").GetBoolean());
                Assert.False((await outsiderConnection.InvokeAsync<JsonElement>("SetTyping", spaceId, true)).GetProperty("ok").GetBoolean());

                var typing = await senderConnection.InvokeAsync<JsonElement>("SetTyping", spaceId, true);
                Assert.True(typing.GetProperty("ok").GetBoolean());
                Assert.True((await started.Task.WaitAsync(TimeSpan.FromSeconds(5))).GetProperty("payload").GetProperty("isTyping").GetBoolean());
                Assert.True((await senderConnection.InvokeAsync<JsonElement>("SetTyping", spaceId, false)).GetProperty("ok").GetBoolean());
                Assert.False((await stopped.Task.WaitAsync(TimeSpan.FromSeconds(5))).GetProperty("payload").GetProperty("isTyping").GetBoolean());
            }
            Assert.Equal(0, await CountMessageOutboxAsync(spaceId));

            await SendTextAsync(sender, spaceId);
            await SendTextAsync(sender, groupId);
            var visibleGroups = await SendAsync(HttpMethod.Get, "/api/v1/conversations/group", reader.Token);
            var visibleGroup = (await visibleGroups.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().Single();
            Assert.Equal(1, visibleGroup.GetProperty("unreadCount").GetInt32());
            Assert.Equal(0, visibleGroup.GetProperty("notificationCount").GetInt32());
            Assert.False(visibleGroup.GetProperty("preferences").GetProperty("isHidden").GetBoolean());
            var visibleInbox = await SendAsync(HttpMethod.Get, "/api/v1/spaces", reader.Token);
            var visible = (await visibleInbox.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray().Single();
            Assert.Equal(1, visible.GetProperty("unreadCount").GetInt32());
            Assert.Equal(0, visible.GetProperty("notificationCount").GetInt32());
            Assert.False(visible.GetProperty("preferences").GetProperty("isHidden").GetBoolean());
            Assert.True(visible.GetProperty("preferences").GetProperty("isPinned").GetBoolean());
            Assert.Equal(HttpStatusCode.OK,
                (await SendAsync(HttpMethod.Put, $"/api/v1/spaces/{spaceId}/preferences", reader.Token,
                    new { notificationLevel = 2, mutedUntil = (DateTimeOffset?)null, isHidden = false, isPinned = true })).StatusCode);
            visibleInbox = await SendAsync(HttpMethod.Get, "/api/v1/spaces", reader.Token);
            visible = (await visibleInbox.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray().Single();
            Assert.Equal(1, visible.GetProperty("unreadCount").GetInt32());
            Assert.Equal(1, visible.GetProperty("notificationCount").GetInt32());
        }
        finally
        {
            await CleanupAsync(actors);
        }
    }

    private async Task<Guid> CreateChannelAsync(TestActor owner, Guid serverId, string name, short visibility)
    {
        var response = await SendAsync(HttpMethod.Post, $"/api/v1/servers/{serverId}/channels", owner.Token,
            new { name, visibility });
        Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("spaceId").GetGuid();
    }

    private async Task SetUserOverrideAsync(TestActor owner, Guid serverId, Guid spaceId, Guid userId, string code)
    {
        var response = await SendAsync(HttpMethod.Put,
            $"/api/v1/servers/{serverId}/channels/{spaceId}/overrides", owner.Token,
            new { userId, permissionCode = code, effect = 1 });
        Assert.True(response.StatusCode == HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
    }

    private async Task<JsonElement> SendTextAsync(TestActor actor, Guid spaceId)
    {
        var response = await SendAsync(HttpMethod.Post, $"/api/v1/spaces/{spaceId}/messages", actor.Token,
            new { clientMessageId = Guid.NewGuid(), messageType = 1, content = "shared message path" });
        Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<TestActor> CreateActorAsync(string label)
    {
        var username = $"unified_{label}_{Guid.NewGuid():N}"[..30];
        const string password = "Messaging123";
        var registration = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new { username, displayName = label, email = $"{username}@example.test", password });
        Assert.True(registration.StatusCode == HttpStatusCode.Created, await registration.Content.ReadAsStringAsync());
        var body = await registration.Content.ReadFromJsonAsync<JsonElement>();
        var verification = await _client.PostAsJsonAsync("/api/v1/auth/verify-email",
            new { token = body.GetProperty("developmentVerificationToken").GetString() });
        Assert.Equal(HttpStatusCode.NoContent, verification.StatusCode);
        var login = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { login = username, password, deviceName = "Unified space test" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var tokens = await login.Content.ReadFromJsonAsync<JsonElement>();
        return new(body.GetProperty("userId").GetGuid(), tokens.GetProperty("accessToken").GetString()!, username);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string token, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }

    private HubConnection CreateHubConnection(string token) => new HubConnectionBuilder()
        .WithUrl(new Uri(_client.BaseAddress!, "/hubs/chat"), options =>
        {
            options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            options.Transports = HttpTransportType.LongPolling;
            options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
        })
        .Build();

    private async Task<int> CountMessageOutboxAsync(Guid spaceId)
    {
        var connectionString = factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("Database")!;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT count(*) FROM integration.outbox_events WHERE space_id = @space_id AND event_type = 'Messaging.MessageCreated'", connection);
        command.Parameters.AddWithValue("space_id", spaceId);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private async Task InsertExcludedMessagesAsync(Guid spaceId, Guid authorId, Guid rootMessageId)
    {
        var connectionString = factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("Database")!;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            INSERT INTO messaging.messages (space_id, message_type, content)
            VALUES (@space_id, 2, 'system');
            INSERT INTO messaging.messages (space_id, author_user_id, client_message_id, message_type, content, created_at, deleted_at)
            VALUES (@space_id, @author_id, @deleted_client_id, 1, 'deleted', @deleted_at, @deleted_at);
            INSERT INTO messaging.messages (space_id, author_user_id, client_message_id, message_type, content, thread_root_id)
            VALUES (@space_id, @author_id, @thread_client_id, 1, 'thread reply', @root_id);
            """, connection);
        command.Parameters.AddWithValue("space_id", spaceId);
        command.Parameters.AddWithValue("author_id", authorId);
        command.Parameters.AddWithValue("deleted_client_id", Guid.NewGuid());
        command.Parameters.AddWithValue("thread_client_id", Guid.NewGuid());
        command.Parameters.AddWithValue("deleted_at", DateTimeOffset.UtcNow);
        command.Parameters.AddWithValue("root_id", rootMessageId);
        await command.ExecuteNonQueryAsync();
    }

    private async Task CleanupAsync(IReadOnlyCollection<TestActor> actors)
    {
        if (actors.Count == 0) return;
        var ids = actors.Select(actor => actor.Id).ToArray();
        var connectionString = factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("Database")!;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        foreach (var sql in new[]
                 {
                     "DELETE FROM integration.outbox_events WHERE space_id IN (SELECT id FROM messaging.spaces WHERE created_by_user_id = ANY(@ids))",
                     "DELETE FROM messaging.messages WHERE thread_root_id IS NOT NULL AND space_id IN (SELECT id FROM messaging.spaces WHERE created_by_user_id = ANY(@ids))",
                     "DELETE FROM messaging.messages WHERE space_id IN (SELECT id FROM messaging.spaces WHERE created_by_user_id = ANY(@ids))",
                     "DELETE FROM community.channels WHERE server_id IN (SELECT id FROM community.servers WHERE owner_user_id = ANY(@ids))",
                     "DELETE FROM community.invites WHERE server_id IN (SELECT id FROM community.servers WHERE owner_user_id = ANY(@ids))",
                     "DELETE FROM community.servers WHERE owner_user_id = ANY(@ids)",
                     "DELETE FROM messaging.space_user_states WHERE space_id IN (SELECT id FROM messaging.spaces WHERE created_by_user_id = ANY(@ids))",
                     "DELETE FROM messaging.space_members WHERE space_id IN (SELECT id FROM messaging.spaces WHERE created_by_user_id = ANY(@ids))",
                     "DELETE FROM messaging.group_conversations WHERE space_id IN (SELECT id FROM messaging.spaces WHERE created_by_user_id = ANY(@ids))",
                     "DELETE FROM messaging.direct_conversations WHERE space_id IN (SELECT id FROM messaging.spaces WHERE created_by_user_id = ANY(@ids))",
                     "DELETE FROM messaging.spaces WHERE created_by_user_id = ANY(@ids)",
                     "DELETE FROM audit.security_events WHERE user_id = ANY(@ids)",
                     "DELETE FROM identity.users WHERE id = ANY(@ids)"
                 })
        {
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("ids", ids);
            await command.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();
    }

    private sealed record TestActor(Guid Id, string Token, string Username);
}
