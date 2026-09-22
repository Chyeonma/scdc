using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SCDC.Api.Tests.Infrastructure;

namespace SCDC.Api.Tests.Community;

public sealed class ChannelAccessFlowTests(SCDCWebApplicationFactory factory) : IClassFixture<SCDCWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Channel_space_is_provisioned_and_http_access_is_revoked_after_kick()
    {
        var actors = new List<TestActor>();
        try
        {
            var owner = await CreateActorAsync("owner");
            var member = await CreateActorAsync("member");
            actors.AddRange([owner, member]);

            var createServer = await SendAsync(HttpMethod.Post, "/api/v1/servers", owner.Token, new { name = "Channel Test Server", description = "test" });
            Assert.True(createServer.StatusCode == HttpStatusCode.Created, await createServer.Content.ReadAsStringAsync());
            var serverId = (await createServer.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

            var createChannel = await SendAsync(HttpMethod.Post, $"/api/v1/servers/{serverId}/channels", owner.Token, new { name = "general", topic = "test", visibility = 1 });
            Assert.True(createChannel.StatusCode == HttpStatusCode.Created, await createChannel.Content.ReadAsStringAsync());
            var channel = await createChannel.Content.ReadFromJsonAsync<JsonElement>();
            var spaceId = channel.GetProperty("spaceId").GetGuid();
            Assert.True(channel.GetProperty("canRead").GetBoolean());
            Assert.True(channel.GetProperty("canSend").GetBoolean());

            var outsiderHistory = await SendAsync(HttpMethod.Get, $"/api/v1/spaces/{spaceId}/messages?limit=20", member.Token);
            Assert.Equal(HttpStatusCode.NotFound, outsiderHistory.StatusCode);

            var invite = await SendAsync(HttpMethod.Post, $"/api/v1/servers/{serverId}/invites", owner.Token, new { });
            Assert.Equal(HttpStatusCode.Created, invite.StatusCode);
            var code = (await invite.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();
            var join = await SendAsync(HttpMethod.Post, $"/api/v1/invites/{code}/join", member.Token, new { });
            Assert.Equal(HttpStatusCode.OK, join.StatusCode);

            var send = await SendAsync(HttpMethod.Post, $"/api/v1/spaces/{spaceId}/messages", member.Token, new { clientMessageId = Guid.NewGuid(), messageType = 1, content = "hello channel" });
            Assert.Equal(HttpStatusCode.Created, send.StatusCode);
            var history = await SendAsync(HttpMethod.Get, $"/api/v1/spaces/{spaceId}/messages?limit=20", member.Token);
            Assert.Equal(HttpStatusCode.OK, history.StatusCode);

            var kick = await SendAsync(HttpMethod.Post, $"/api/v1/servers/{serverId}/members/{member.UserId}/kick", owner.Token, new { });
            Assert.Equal(HttpStatusCode.NoContent, kick.StatusCode);
            var revokedHistory = await SendAsync(HttpMethod.Get, $"/api/v1/spaces/{spaceId}/messages?limit=20", member.Token);
            Assert.Equal(HttpStatusCode.NotFound, revokedHistory.StatusCode);
        }
        finally { await CleanupAsync(actors); }
    }

    private async Task<TestActor> CreateActorAsync(string label)
    {
        var username = $"channel_{label}_{Guid.NewGuid():N}"[..30];
        var registration = await _client.PostAsJsonAsync("/api/v1/auth/register", new { username, displayName = label, email = $"{username}@example.test", password = "Channel123" });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        var body = await registration.Content.ReadFromJsonAsync<JsonElement>();
        await _client.PostAsJsonAsync("/api/v1/auth/verify-email", new { token = body.GetProperty("developmentVerificationToken").GetString() });
        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new { login = username, password = "Channel123", deviceName = "test" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var tokens = await login.Content.ReadFromJsonAsync<JsonElement>();
        return new(body.GetProperty("userId").GetGuid(), tokens.GetProperty("accessToken").GetString()!);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string token, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path) { Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) } };
        if (body is not null) request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }

    private async Task CleanupAsync(IReadOnlyCollection<TestActor> actors)
    {
        if (actors.Count == 0) return;
        var connectionString = factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("Database")!;
        var ids = actors.Select(x => x.UserId).ToArray();
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync(); await using var transaction = await connection.BeginTransactionAsync();
        foreach (var sql in new[] {
            "DELETE FROM integration.outbox_events WHERE space_id IN (SELECT id FROM messaging.spaces WHERE created_by_user_id = ANY(@ids))",
            "DELETE FROM messaging.messages WHERE author_user_id = ANY(@ids)",
            "DELETE FROM community.channels WHERE server_id IN (SELECT id FROM community.servers WHERE owner_user_id = ANY(@ids))",
            "DELETE FROM community.invites WHERE server_id IN (SELECT id FROM community.servers WHERE owner_user_id = ANY(@ids))",
            "DELETE FROM community.servers WHERE owner_user_id = ANY(@ids)",
            "DELETE FROM messaging.spaces WHERE created_by_user_id = ANY(@ids)",
            "DELETE FROM audit.security_events WHERE user_id = ANY(@ids)",
            "DELETE FROM identity.users WHERE id = ANY(@ids)" })
        { await using var command = new NpgsqlCommand(sql, connection, transaction); command.Parameters.AddWithValue("ids", ids); await command.ExecuteNonQueryAsync(); }
        await transaction.CommitAsync();
    }

    private sealed record TestActor(Guid UserId, string Token);
}
