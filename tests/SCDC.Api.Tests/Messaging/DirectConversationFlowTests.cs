using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SCDC.Api.Tests.Infrastructure;

namespace SCDC.Api.Tests.Messaging;

public sealed class DirectConversationFlowTests(SCDCWebApplicationFactory factory)
    : IClassFixture<SCDCWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Direct_conversation_is_created_once_and_only_members_can_read_it()
    {
        var actors = new List<TestActor>();
        try
        {
            var actorA = await CreateActorAsync("A");
            var actorB = await CreateActorAsync("B");
            var actorC = await CreateActorAsync("C");
            actors.AddRange([actorA, actorB, actorC]);

            var create = await SendAuthorizedAsync(
                HttpMethod.Post,
                "/api/v1/conversations/direct",
                actorA.AccessToken,
                new { recipientUserId = actorB.UserId });

            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var createdSpace = await create.Content.ReadFromJsonAsync<JsonElement>();
            var spaceId = createdSpace.GetProperty("id").GetGuid();
            Assert.Equal((short)1, createdSpace.GetProperty("spaceType").GetInt16());
            Assert.Equal(actorB.UserId, createdSpace.GetProperty("peer").GetProperty("id").GetGuid());
            Assert.Equal($"/api/v1/spaces/{spaceId}", create.Headers.Location?.OriginalString);

            var getExisting = await SendAuthorizedAsync(
                HttpMethod.Post,
                "/api/v1/conversations/direct",
                actorB.AccessToken,
                new { recipientUserId = actorA.UserId });

            Assert.Equal(HttpStatusCode.OK, getExisting.StatusCode);
            var existingSpace = await getExisting.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(spaceId, existingSpace.GetProperty("id").GetGuid());
            Assert.Equal(actorA.UserId, existingSpace.GetProperty("peer").GetProperty("id").GetGuid());

            var memberRead = await SendAuthorizedAsync(
                HttpMethod.Get,
                $"/api/v1/spaces/{spaceId}",
                actorB.AccessToken);
            Assert.Equal(HttpStatusCode.OK, memberRead.StatusCode);

            var outsiderRead = await SendAuthorizedAsync(
                HttpMethod.Get,
                $"/api/v1/spaces/{spaceId}",
                actorC.AccessToken);
            Assert.Equal(HttpStatusCode.NotFound, outsiderRead.StatusCode);
            var outsiderProblem = await outsiderRead.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Messaging.ResourceNotFound", outsiderProblem.GetProperty("errorCode").GetString());

            var selfConversation = await SendAuthorizedAsync(
                HttpMethod.Post,
                "/api/v1/conversations/direct",
                actorA.AccessToken,
                new { recipientUserId = actorA.UserId });
            Assert.Equal(HttpStatusCode.BadRequest, selfConversation.StatusCode);
            var selfProblem = await selfConversation.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Messaging.SelfConversationNotAllowed", selfProblem.GetProperty("errorCode").GetString());

            var missingRecipient = await SendAuthorizedAsync(
                HttpMethod.Post,
                "/api/v1/conversations/direct",
                actorA.AccessToken,
                new { recipientUserId = Guid.Empty });
            Assert.Equal(HttpStatusCode.BadRequest, missingRecipient.StatusCode);
            var missingRecipientProblem = await missingRecipient.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Messaging.ValidationFailed", missingRecipientProblem.GetProperty("errorCode").GetString());

            var counts = await GetDirectConversationCountsAsync(spaceId);
            Assert.Equal(1, counts.DirectConversations);
            Assert.Equal(2, counts.ActiveMembers);
            Assert.Equal(2, counts.UserStates);
        }
        finally
        {
            await CleanupActorsAsync(actors);
        }
    }

    [Fact]
    public async Task Concurrent_opens_from_both_sides_return_the_same_direct_conversation()
    {
        var actors = new List<TestActor>();
        try
        {
            var actorA = await CreateActorAsync("ConcurrentA");
            var actorB = await CreateActorAsync("ConcurrentB");
            actors.AddRange([actorA, actorB]);

            var openByA = SendAuthorizedAsync(
                HttpMethod.Post,
                "/api/v1/conversations/direct",
                actorA.AccessToken,
                new { recipientUserId = actorB.UserId });
            var openByB = SendAuthorizedAsync(
                HttpMethod.Post,
                "/api/v1/conversations/direct",
                actorB.AccessToken,
                new { recipientUserId = actorA.UserId });

            var responses = await Task.WhenAll(openByA, openByB);
            Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.Created);
            Assert.All(responses, response => Assert.True(
                response.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK));

            var spaces = await Task.WhenAll(responses.Select(async response =>
            {
                var body = await response.Content.ReadFromJsonAsync<JsonElement>();
                return body.GetProperty("id").GetGuid();
            }));
            Assert.Single(spaces.Distinct());

            var counts = await GetDirectConversationCountsAsync(spaces[0]);
            Assert.Equal(1, counts.DirectConversations);
            Assert.Equal(2, counts.ActiveMembers);
            Assert.Equal(2, counts.UserStates);
        }
        finally
        {
            await CleanupActorsAsync(actors);
        }
    }

    private async Task<TestActor> CreateActorAsync(string label)
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var username = $"msg_{label.ToLowerInvariant()}_{suffix}";
        var email = $"{username}@example.test";
        const string password = "Messaging123";

        var registration = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            username,
            displayName = $"Messaging Test {label}",
            email,
            password
        });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        var registrationBody = await registration.Content.ReadFromJsonAsync<JsonElement>();
        var verificationToken = registrationBody.GetProperty("developmentVerificationToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(verificationToken));

        var verification = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", new { token = verificationToken });
        Assert.Equal(HttpStatusCode.NoContent, verification.StatusCode);

        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            login = username,
            password,
            deviceName = "Messaging integration test"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var loginBody = await login.Content.ReadFromJsonAsync<JsonElement>();

        return new TestActor(
            registrationBody.GetProperty("userId").GetGuid(),
            username,
            loginBody.GetProperty("accessToken").GetString()!);
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(
        HttpMethod method,
        string path,
        string accessToken,
        object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }

    private async Task<(int DirectConversations, int ActiveMembers, int UserStates)> GetDirectConversationCountsAsync(Guid spaceId)
    {
        var connectionString = factory.Services.GetRequiredService<IConfiguration>()
            .GetConnectionString("Database")
            ?? throw new InvalidOperationException("Test database connection is missing.");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT
                (SELECT count(*) FROM messaging.direct_conversations WHERE space_id = @space_id),
                (SELECT count(*) FROM messaging.space_members WHERE space_id = @space_id AND membership_status = 1),
                (SELECT count(*) FROM messaging.space_user_states WHERE space_id = @space_id)
            """,
            connection);
        command.Parameters.AddWithValue("space_id", spaceId);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return (reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2));
    }

    private async Task CleanupActorsAsync(IReadOnlyCollection<TestActor> actors)
    {
        if (actors.Count == 0)
        {
            return;
        }

        var connectionString = factory.Services.GetRequiredService<IConfiguration>()
            .GetConnectionString("Database")
            ?? throw new InvalidOperationException("Test database connection is missing.");
        var userIds = actors.Select(actor => actor.UserId).ToArray();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        foreach (var sql in new[]
                 {
                     "DELETE FROM messaging.space_user_states WHERE user_id = ANY(@user_ids)",
                     "DELETE FROM messaging.user_blocks WHERE blocker_user_id = ANY(@user_ids) OR blocked_user_id = ANY(@user_ids)",
                     "DELETE FROM messaging.direct_conversations WHERE user_low_id = ANY(@user_ids) OR user_high_id = ANY(@user_ids)",
                     "DELETE FROM messaging.space_members WHERE user_id = ANY(@user_ids)",
                     "DELETE FROM messaging.spaces WHERE created_by_user_id = ANY(@user_ids)",
                     "DELETE FROM integration.outbox_events WHERE aggregate_id = ANY(@user_ids)",
                     "DELETE FROM audit.security_events WHERE user_id = ANY(@user_ids)",
                     "DELETE FROM identity.users WHERE id = ANY(@user_ids)"
                 })
        {
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("user_ids", userIds);
            await command.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }

    private sealed record TestActor(Guid UserId, string Username, string AccessToken);
}
