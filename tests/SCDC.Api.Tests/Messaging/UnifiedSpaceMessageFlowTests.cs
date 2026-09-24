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
        return new(body.GetProperty("userId").GetGuid(), tokens.GetProperty("accessToken").GetString()!);
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

    private sealed record TestActor(Guid Id, string Token);
}
