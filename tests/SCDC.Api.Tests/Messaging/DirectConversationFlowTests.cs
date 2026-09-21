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

    [Fact]
    public async Task Inbox_is_paged_and_only_lists_the_callers_direct_conversations()
    {
        var actors = new List<TestActor>();
        try
        {
            var actorA = await CreateActorAsync("InboxA");
            var actorB = await CreateActorAsync("InboxB");
            var actorC = await CreateActorAsync("InboxC");
            actors.AddRange([actorA, actorB, actorC]);

            await CreateDirectConversationAsync(actorA, actorB);
            await CreateDirectConversationAsync(actorA, actorC);

            var firstPage = await SendAuthorizedAsync(
                HttpMethod.Get,
                "/api/v1/spaces?limit=1",
                actorA.AccessToken);
            Assert.Equal(HttpStatusCode.OK, firstPage.StatusCode);
            var firstPageBody = await firstPage.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, firstPageBody.GetProperty("items").GetArrayLength());
            Assert.True(firstPageBody.GetProperty("hasMore").GetBoolean());
            var nextCursor = firstPageBody.GetProperty("nextCursor").GetString();
            Assert.False(string.IsNullOrWhiteSpace(nextCursor));

            var secondPage = await SendAuthorizedAsync(
                HttpMethod.Get,
                $"/api/v1/spaces?limit=1&cursor={Uri.EscapeDataString(nextCursor!)}",
                actorA.AccessToken);
            Assert.Equal(HttpStatusCode.OK, secondPage.StatusCode);
            var secondPageBody = await secondPage.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, secondPageBody.GetProperty("items").GetArrayLength());
            Assert.False(secondPageBody.GetProperty("hasMore").GetBoolean());

            var inboxIds = new[]
                {
                    firstPageBody.GetProperty("items")[0].GetProperty("id").GetGuid(),
                    secondPageBody.GetProperty("items")[0].GetProperty("id").GetGuid()
                };
            Assert.Equal(2, inboxIds.Distinct().Count());

            var recipient = await SendAuthorizedAsync(
                HttpMethod.Get,
                $"/api/v1/conversations/direct/recipient?username={actorB.Username}",
                actorA.AccessToken);
            Assert.Equal(HttpStatusCode.OK, recipient.StatusCode);
            var recipientBody = await recipient.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(actorB.UserId, recipientBody.GetProperty("id").GetGuid());
            Assert.Equal(actorB.Username, recipientBody.GetProperty("username").GetString());

            var actorBInbox = await SendAuthorizedAsync(
                HttpMethod.Get,
                "/api/v1/spaces?limit=100",
                actorB.AccessToken);
            Assert.Equal(HttpStatusCode.OK, actorBInbox.StatusCode);
            var actorBItems = (await actorBInbox.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("items");
            Assert.Single(actorBItems.EnumerateArray());
        }
        finally
        {
            await CleanupActorsAsync(actors);
        }
    }

    [Fact]
    public async Task Sending_a_message_is_idempotent_and_updates_the_space_and_outbox_together()
    {
        var actors = new List<TestActor>();
        try
        {
            var actorA = await CreateActorAsync("SendA");
            var actorB = await CreateActorAsync("SendB");
            actors.AddRange([actorA, actorB]);

            var spaceId = await CreateDirectConversationAsync(actorA, actorB);
            var clientMessageId = Guid.NewGuid();
            var firstSend = await SendAuthorizedAsync(
                HttpMethod.Post,
                $"/api/v1/spaces/{spaceId}/messages",
                actorA.AccessToken,
                new { clientMessageId, messageType = 1, content = "  First\r\nmessage  " });
            Assert.Equal(HttpStatusCode.Created, firstSend.StatusCode);
            var firstMessage = await firstSend.Content.ReadFromJsonAsync<JsonElement>();
            var messageId = firstMessage.GetProperty("id").GetGuid();
            Assert.Equal("First\nmessage", firstMessage.GetProperty("content").GetString());
            Assert.Equal(clientMessageId, firstMessage.GetProperty("clientMessageId").GetGuid());

            var retry = await SendAuthorizedAsync(
                HttpMethod.Post,
                $"/api/v1/spaces/{spaceId}/messages",
                actorA.AccessToken,
                new { clientMessageId, messageType = 1, content = "  First\r\nmessage  " });
            Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
            var retryMessage = await retry.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(messageId, retryMessage.GetProperty("id").GetGuid());

            var conflictingRetry = await SendAuthorizedAsync(
                HttpMethod.Post,
                $"/api/v1/spaces/{spaceId}/messages",
                actorA.AccessToken,
                new { clientMessageId, messageType = 1, content = "Changed payload" });
            Assert.Equal(HttpStatusCode.Conflict, conflictingRetry.StatusCode);
            var conflictProblem = await conflictingRetry.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Messaging.IdempotencyConflict", conflictProblem.GetProperty("errorCode").GetString());

            var state = await GetMessagePersistenceAsync(spaceId, messageId);
            Assert.Equal(1, state.Messages);
            Assert.Equal(1, state.MessageCreatedOutboxEvents);
            Assert.Equal(messageId, state.LastMessageId);
            Assert.Equal(firstMessage.GetProperty("sequenceNo").GetString(), state.LastMessageSequence);
        }
        finally
        {
            await CleanupActorsAsync(actors);
        }
    }

    [Fact]
    public async Task Concurrent_sends_are_serialized_and_leave_the_latest_sequence_as_the_space_projection()
    {
        var actors = new List<TestActor>();
        try
        {
            var actorA = await CreateActorAsync("SequenceA");
            var actorB = await CreateActorAsync("SequenceB");
            actors.AddRange([actorA, actorB]);
            var spaceId = await CreateDirectConversationAsync(actorA, actorB);

            var sends = await Task.WhenAll(
                SendAuthorizedAsync(HttpMethod.Post, $"/api/v1/spaces/{spaceId}/messages", actorA.AccessToken,
                    new { clientMessageId = Guid.NewGuid(), messageType = 1, content = "First concurrent send" }),
                SendAuthorizedAsync(HttpMethod.Post, $"/api/v1/spaces/{spaceId}/messages", actorB.AccessToken,
                    new { clientMessageId = Guid.NewGuid(), messageType = 1, content = "Second concurrent send" }));
            Assert.All(sends, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));

            var messages = await Task.WhenAll(sends.Select(response => response.Content.ReadFromJsonAsync<JsonElement>()));
            var latest = messages
                .OrderBy(message => long.Parse(message.GetProperty("sequenceNo").GetString()!))
                .Last();
            var state = await GetMessagePersistenceAsync(spaceId, latest.GetProperty("id").GetGuid());
            Assert.Equal(2, state.Messages);
            Assert.Equal(2, state.MessageCreatedOutboxEvents);
            Assert.Equal(latest.GetProperty("id").GetGuid(), state.LastMessageId);
            Assert.Equal(latest.GetProperty("sequenceNo").GetString(), state.LastMessageSequence);
        }
        finally
        {
            await CleanupActorsAsync(actors);
        }
    }

    [Fact]
    public async Task History_uses_stable_before_and_after_cursors_and_hides_tombstone_content()
    {
        var actors = new List<TestActor>();
        try
        {
            var actorA = await CreateActorAsync("HistoryA");
            var actorB = await CreateActorAsync("HistoryB");
            var actorC = await CreateActorAsync("HistoryC");
            actors.AddRange([actorA, actorB, actorC]);
            var spaceId = await CreateDirectConversationAsync(actorA, actorB);

            var first = await SendTextAsync(actorA, spaceId, "First");
            var second = await SendTextAsync(actorB, spaceId, "Second");
            var third = await SendTextAsync(actorA, spaceId, "Third");
            await MarkMessageDeletedAsync(second.GetProperty("id").GetGuid());

            var firstHistory = await SendAuthorizedAsync(
                HttpMethod.Get,
                $"/api/v1/spaces/{spaceId}/messages?limit=2",
                actorA.AccessToken);
            Assert.Equal(HttpStatusCode.OK, firstHistory.StatusCode);
            var firstPage = await firstHistory.Content.ReadFromJsonAsync<JsonElement>();
            var firstItems = firstPage.GetProperty("items").EnumerateArray().ToArray();
            Assert.Equal(2, firstItems.Length);
            Assert.True(firstPage.GetProperty("hasMore").GetBoolean());
            Assert.True(long.Parse(firstItems[0].GetProperty("sequenceNo").GetString()!)
                        < long.Parse(firstItems[1].GetProperty("sequenceNo").GetString()!));
            Assert.Equal(second.GetProperty("id").GetGuid(), firstItems[0].GetProperty("id").GetGuid());
            Assert.True(firstItems[0].GetProperty("deletedAt").GetDateTimeOffset() <= DateTimeOffset.UtcNow);
            Assert.Equal(JsonValueKind.Null, firstItems[0].GetProperty("content").ValueKind);

            var nextBefore = firstPage.GetProperty("nextBeforeSequence").GetString();
            var olderHistory = await SendAuthorizedAsync(
                HttpMethod.Get,
                $"/api/v1/spaces/{spaceId}/messages?limit=2&beforeSequence={nextBefore}",
                actorA.AccessToken);
            var olderPage = await olderHistory.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Single(olderPage.GetProperty("items").EnumerateArray());
            Assert.Equal(first.GetProperty("id").GetGuid(), olderPage.GetProperty("items")[0].GetProperty("id").GetGuid());

            var catchUp = await SendAuthorizedAsync(
                HttpMethod.Get,
                $"/api/v1/spaces/{spaceId}/messages?limit=1&afterSequence={first.GetProperty("sequenceNo").GetString()}",
                actorA.AccessToken);
            var catchUpPage = await catchUp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(catchUpPage.GetProperty("hasMore").GetBoolean());
            Assert.Equal(second.GetProperty("id").GetGuid(), catchUpPage.GetProperty("items")[0].GetProperty("id").GetGuid());
            var highWatermark = catchUpPage.GetProperty("highWatermark").GetString();
            var nextAfter = catchUpPage.GetProperty("nextAfterSequence").GetString();
            var fourth = await SendTextAsync(actorB, spaceId, "Fourth after catch-up started");

            var finalCatchUp = await SendAuthorizedAsync(
                HttpMethod.Get,
                $"/api/v1/spaces/{spaceId}/messages?limit=1&afterSequence={nextAfter}&throughSequence={highWatermark}",
                actorA.AccessToken);
            var finalCatchUpPage = await finalCatchUp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(finalCatchUpPage.GetProperty("hasMore").GetBoolean());
            Assert.Equal(third.GetProperty("id").GetGuid(), finalCatchUpPage.GetProperty("items")[0].GetProperty("id").GetGuid());

            var latestCatchUp = await SendAuthorizedAsync(
                HttpMethod.Get,
                $"/api/v1/spaces/{spaceId}/messages?limit=1&afterSequence={highWatermark}",
                actorA.AccessToken);
            var latestCatchUpPage = await latestCatchUp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(latestCatchUpPage.GetProperty("hasMore").GetBoolean());
            Assert.Equal(fourth.GetProperty("id").GetGuid(), latestCatchUpPage.GetProperty("items")[0].GetProperty("id").GetGuid());

            var outsiderHistory = await SendAuthorizedAsync(HttpMethod.Get, $"/api/v1/spaces/{spaceId}/messages", actorC.AccessToken);
            Assert.Equal(HttpStatusCode.NotFound, outsiderHistory.StatusCode);
        }
        finally
        {
            await CleanupActorsAsync(actors);
        }
    }

    [Fact]
    public async Task Realtime_subscribe_requires_current_space_membership()
    {
        var actors = new List<TestActor>();
        try
        {
            var actorA = await CreateActorAsync("HubA");
            var actorB = await CreateActorAsync("HubB");
            var actorC = await CreateActorAsync("HubC");
            actors.AddRange([actorA, actorB, actorC]);
            var spaceId = await CreateDirectConversationAsync(actorA, actorB);

            await using var member = CreateHubConnection(actorA.AccessToken);
            await using var outsider = CreateHubConnection(actorC.AccessToken);
            await member.StartAsync();
            await outsider.StartAsync();

            var allowed = await member.InvokeAsync<HubResultResponse>("SubscribeSpace", spaceId);
            Assert.True(allowed.Ok);
            Assert.Equal(spaceId, allowed.Value?.SpaceId);
            Assert.NotNull(allowed.Value?.HighWatermark);

            var denied = await outsider.InvokeAsync<HubResultResponse>("SubscribeSpace", spaceId);
            Assert.False(denied.Ok);
            Assert.Equal("Messaging.ResourceNotFound", denied.Error?.ErrorCode);

            var unsubscribed = await member.InvokeAsync<HubResultResponse>("UnsubscribeSpace", spaceId);
            Assert.True(unsubscribed.Ok);
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

    private HubConnection CreateHubConnection(string accessToken) => new HubConnectionBuilder()
        .WithUrl(new Uri(_client.BaseAddress!, "/hubs/chat"), options =>
        {
            options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
            options.Transports = HttpTransportType.LongPolling;
            options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
        })
        .Build();

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

    private async Task<Guid> CreateDirectConversationAsync(TestActor actor, TestActor recipient)
    {
        var response = await SendAuthorizedAsync(
            HttpMethod.Post,
            "/api/v1/conversations/direct",
            actor.AccessToken,
            new { recipientUserId = recipient.UserId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    private async Task<JsonElement> SendTextAsync(TestActor actor, Guid spaceId, string content)
    {
        var response = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"/api/v1/spaces/{spaceId}/messages",
            actor.AccessToken,
            new { clientMessageId = Guid.NewGuid(), messageType = 1, content });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task MarkMessageDeletedAsync(Guid messageId)
    {
        var connectionString = factory.Services.GetRequiredService<IConfiguration>()
            .GetConnectionString("Database")
            ?? throw new InvalidOperationException("Test database connection is missing.");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "UPDATE messaging.messages SET content = '[deleted]', deleted_at = clock_timestamp() WHERE id = @message_id",
            connection);
        command.Parameters.AddWithValue("message_id", messageId);
        Assert.Equal(1, await command.ExecuteNonQueryAsync());
    }

    private async Task<(int Messages, int MessageCreatedOutboxEvents, Guid? LastMessageId, string? LastMessageSequence)>
        GetMessagePersistenceAsync(Guid spaceId, Guid messageId)
    {
        var connectionString = factory.Services.GetRequiredService<IConfiguration>()
            .GetConnectionString("Database")
            ?? throw new InvalidOperationException("Test database connection is missing.");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT
                (SELECT count(*) FROM messaging.messages WHERE space_id = @space_id),
                (SELECT count(*) FROM integration.outbox_events WHERE space_id = @space_id AND event_type = 'Messaging.MessageCreated'),
                (SELECT last_message_id FROM messaging.spaces WHERE id = @space_id),
                (SELECT last_message_sequence::text FROM messaging.spaces WHERE id = @space_id)
            """,
            connection);
        command.Parameters.AddWithValue("space_id", spaceId);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return (
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.IsDBNull(2) ? null : reader.GetGuid(2),
            reader.IsDBNull(3) ? null : reader.GetString(3));
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
                     "DELETE FROM integration.outbox_events WHERE space_id IN (SELECT id FROM messaging.spaces WHERE created_by_user_id = ANY(@user_ids))",
                     "DELETE FROM messaging.messages WHERE author_user_id = ANY(@user_ids)",
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
    private sealed record HubResultResponse(bool Ok, HubValue? Value, HubErrorResponse? Error);
    private sealed record HubValue(Guid SpaceId, string HighWatermark);
    private sealed record HubErrorResponse(string ErrorCode, string Message, string TraceId);
}
