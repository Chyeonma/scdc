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

public sealed class GroupConversationFlowTests(SCDCWebApplicationFactory factory)
    : IClassFixture<SCDCWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Group_lifecycle_enforces_membership_roles_limits_and_message_access()
    {
        var actors = new List<TestActor>();
        try
        {
            var owner = await CreateActorAsync("GroupOwner");
            var admin = await CreateActorAsync("GroupAdmin");
            var member = await CreateActorAsync("GroupMember");
            var outsider = await CreateActorAsync("GroupOutsider");
            actors.AddRange([owner, admin, member, outsider]);

            var create = await SendAuthorizedAsync(HttpMethod.Post, "/api/v1/conversations/group", owner.AccessToken, new
            {
                name = "  Integration group  ",
                memberUserIds = new[] { admin.UserId, member.UserId },
                maxMembers = 3,
            });
            Assert.True(create.StatusCode == HttpStatusCode.Created, await create.Content.ReadAsStringAsync());
            var group = await create.Content.ReadFromJsonAsync<JsonElement>();
            var spaceId = group.GetProperty("spaceId").GetGuid();
            Assert.Equal("Integration group", group.GetProperty("name").GetString());
            Assert.Equal(owner.UserId, group.GetProperty("ownerUserId").GetGuid());
            Assert.Equal(3, group.GetProperty("memberCount").GetInt32());

            var overLimit = await SendAuthorizedAsync(HttpMethod.Post, $"/api/v1/conversations/group/{spaceId}/members", owner.AccessToken, new { userId = outsider.UserId });
            Assert.Equal(HttpStatusCode.Conflict, overLimit.StatusCode);

            var message = await SendAuthorizedAsync(HttpMethod.Post, $"/api/v1/spaces/{spaceId}/messages", member.AccessToken, new
            {
                clientMessageId = Guid.NewGuid(), messageType = 1, content = "group message",
            });
            Assert.True(message.StatusCode == HttpStatusCode.Created, await message.Content.ReadAsStringAsync());

            var outsiderHistory = await SendAuthorizedAsync(HttpMethod.Get, $"/api/v1/spaces/{spaceId}/messages", outsider.AccessToken);
            Assert.Equal(HttpStatusCode.NotFound, outsiderHistory.StatusCode);

            var promote = await SendAuthorizedAsync(HttpMethod.Put, $"/api/v1/conversations/group/{spaceId}/members/{admin.UserId}/role", owner.AccessToken, new { role = 2 });
            Assert.Equal(HttpStatusCode.NoContent, promote.StatusCode);
            await using var memberConnection = CreateHubConnection(member.AccessToken);
            var revoked = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
            memberConnection.On<JsonElement>("RealtimeEvent", realtimeEvent =>
            {
                if (realtimeEvent.GetProperty("eventType").GetString() == "SpaceAccessRevoked") revoked.TrySetResult(realtimeEvent);
            });
            await memberConnection.StartAsync();
            await memberConnection.InvokeAsync<JsonElement>("SubscribeSpace", spaceId);
            var remove = await SendAuthorizedAsync(HttpMethod.Delete, $"/api/v1/conversations/group/{spaceId}/members/{member.UserId}", admin.AccessToken);
            Assert.Equal(HttpStatusCode.NoContent, remove.StatusCode);
            var revokedEvent = await revoked.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(spaceId, revokedEvent.GetProperty("spaceId").GetGuid());
            var removedHistory = await SendAuthorizedAsync(HttpMethod.Get, $"/api/v1/spaces/{spaceId}/messages", member.AccessToken);
            Assert.Equal(HttpStatusCode.NotFound, removedHistory.StatusCode);

            var ownerLeave = await SendAuthorizedAsync(HttpMethod.Post, $"/api/v1/conversations/group/{spaceId}/leave", owner.AccessToken);
            Assert.Equal(HttpStatusCode.Conflict, ownerLeave.StatusCode);
            var transfer = await SendAuthorizedAsync(HttpMethod.Put, $"/api/v1/conversations/group/{spaceId}/owner", owner.AccessToken, new { userId = admin.UserId });
            Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);
            var leave = await SendAuthorizedAsync(HttpMethod.Post, $"/api/v1/conversations/group/{spaceId}/leave", owner.AccessToken);
            Assert.Equal(HttpStatusCode.NoContent, leave.StatusCode);

            var detail = await SendAuthorizedAsync(HttpMethod.Get, $"/api/v1/conversations/group/{spaceId}", admin.AccessToken);
            Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
            var detailBody = await detail.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(admin.UserId, detailBody.GetProperty("ownerUserId").GetGuid());
            Assert.Equal(1, detailBody.GetProperty("memberCount").GetInt32());
        }
        finally
        {
            await CleanupActorsAsync(actors);
        }
    }

    private async Task<TestActor> CreateActorAsync(string label)
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var username = $"group_{label.ToLowerInvariant()}_{suffix}";
        const string password = "Messaging123";
        var registration = await _client.PostAsJsonAsync("/api/v1/auth/register", new { username, displayName = label, email = $"{username}@example.test", password });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        var registrationBody = await registration.Content.ReadFromJsonAsync<JsonElement>();
        var verification = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", new { token = registrationBody.GetProperty("developmentVerificationToken").GetString() });
        Assert.Equal(HttpStatusCode.NoContent, verification.StatusCode);
        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new { login = username, password, deviceName = "Group integration test" });
        var loginBody = await login.Content.ReadFromJsonAsync<JsonElement>();
        return new TestActor(registrationBody.GetProperty("userId").GetGuid(), loginBody.GetProperty("accessToken").GetString()!);
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(HttpMethod method, string path, string accessToken, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }

    private HubConnection CreateHubConnection(string accessToken) => new HubConnectionBuilder()
        .WithUrl(new Uri(_client.BaseAddress!, "/hubs/chat"), options =>
        {
            options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
            options.Transports = HttpTransportType.LongPolling;
            options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
        })
        .Build();

    private async Task CleanupActorsAsync(IReadOnlyCollection<TestActor> actors)
    {
        if (actors.Count == 0) return;
        var connectionString = factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("Database")!;
        var userIds = actors.Select(actor => actor.UserId).ToArray();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        foreach (var sql in new[]
                 {
                     "DELETE FROM integration.outbox_events WHERE space_id IN (SELECT id FROM messaging.spaces WHERE created_by_user_id = ANY(@user_ids))",
                     "DELETE FROM messaging.messages WHERE space_id IN (SELECT id FROM messaging.spaces WHERE created_by_user_id = ANY(@user_ids))",
                     "DELETE FROM messaging.space_user_states WHERE space_id IN (SELECT id FROM messaging.spaces WHERE created_by_user_id = ANY(@user_ids))",
                     "DELETE FROM messaging.space_members WHERE space_id IN (SELECT id FROM messaging.spaces WHERE created_by_user_id = ANY(@user_ids))",
                     "DELETE FROM messaging.group_conversations WHERE owner_user_id = ANY(@user_ids)",
                     "DELETE FROM messaging.spaces WHERE created_by_user_id = ANY(@user_ids)",
                     "DELETE FROM audit.security_events WHERE user_id = ANY(@user_ids)",
                     "DELETE FROM identity.users WHERE id = ANY(@user_ids)",
                 })
        {
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("user_ids", userIds);
            await command.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();
    }

    private sealed record TestActor(Guid UserId, string AccessToken);
}
