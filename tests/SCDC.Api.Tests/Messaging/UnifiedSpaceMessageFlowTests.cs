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
                var denied = await connection.InvokeAsync<JsonElement>("SubscribeSpace", privateId);
                Assert.False(denied.GetProperty("ok").GetBoolean());
            }

            Assert.Equal(HttpStatusCode.OK,
                (await SendAsync(HttpMethod.Get, $"/api/v1/spaces/{readOnlyId}/messages?limit=20", member.Token)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await SendAsync(HttpMethod.Post, $"/api/v1/spaces/{readOnlyId}/messages", member.Token,
                    new { clientMessageId = Guid.NewGuid(), messageType = 1, content = "denied" })).StatusCode);
            await SendTextAsync(owner, readOnlyId);
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
