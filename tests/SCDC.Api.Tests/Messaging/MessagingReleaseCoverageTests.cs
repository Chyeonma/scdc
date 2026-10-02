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
using SCDC.Modules.Messaging.Application;

namespace SCDC.Api.Tests.Messaging;

public sealed class MessagingReleaseCoverageTests
{
    [Fact]
    public async Task Login_to_dm_realtime_refresh_and_reconnect_catch_up_keeps_each_message_once()
    {
        using var factory = new SCDCWebApplicationFactory(useRealtimePublisher: true);
        using var client = factory.CreateClient();
        var actors = new List<TestActor>();
        try
        {
            var sender = await CreateActorAsync(client, "journey_sender");
            var reader = await CreateActorAsync(client, "journey_reader");
            actors.AddRange([sender, reader]);
            var dm = await SendAsync(client, HttpMethod.Post, "/api/v1/conversations/direct", sender.AccessToken,
                new { recipientUserId = reader.Id });
            Assert.Equal(HttpStatusCode.Created, dm.StatusCode);
            var spaceId = (await dm.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

            await using (var receiver = CreateHub(factory, client, reader.AccessToken))
            {
                var received = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
                receiver.On<JsonElement>("RealtimeEvent", envelope =>
                {
                    if (envelope.GetProperty("eventType").GetString() == "MessageCreated"
                        && envelope.GetProperty("spaceId").GetGuid() == spaceId)
                        received.TrySetResult(envelope);
                });
                await receiver.StartAsync();
                Assert.True((await receiver.InvokeAsync<JsonElement>("SubscribeSpace", spaceId))
                    .GetProperty("ok").GetBoolean());
                var first = await SendTextAsync(client, sender, spaceId, "release journey first");
                using (var scope = factory.Services.CreateScope())
                    Assert.True(await scope.ServiceProvider.GetRequiredService<IMessagingOutboxDispatcher>()
                        .DispatchDueAsync(CancellationToken.None) >= 1);
                var envelope = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.Equal(first.GetProperty("id").GetGuid(),
                    envelope.GetProperty("payload").GetProperty("messageId").GetGuid());

                var refresh = await client.PostAsJsonAsync("/api/v1/auth/refresh",
                    new { refreshToken = reader.RefreshToken });
                Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
                var refreshed = await refresh.Content.ReadFromJsonAsync<JsonElement>();
                var newToken = refreshed.GetProperty("accessToken").GetString()!;
                using var refreshedPage = factory.CreateClient();
                var initialHistory = await SendAsync(refreshedPage, HttpMethod.Get,
                    $"/api/v1/spaces/{spaceId}/messages?limit=50", newToken);
                Assert.Equal(HttpStatusCode.OK, initialHistory.StatusCode);
                var firstPage = await initialHistory.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Single(firstPage.GetProperty("items").EnumerateArray());
                Assert.Equal(first.GetProperty("id").GetGuid(),
                    firstPage.GetProperty("items")[0].GetProperty("id").GetGuid());

                // The receiver disconnects before the next message. Re-subscribe, then catch up
                // from the last sequence rather than relying on event replay.
                await receiver.StopAsync();
                var second = await SendTextAsync(client, sender, spaceId, "release journey second");
                using (var scope = factory.Services.CreateScope())
                    await scope.ServiceProvider.GetRequiredService<IMessagingOutboxDispatcher>()
                        .DispatchDueAsync(CancellationToken.None);
                await using var reconnected = CreateHub(factory, refreshedPage, newToken);
                await reconnected.StartAsync();
                Assert.True((await reconnected.InvokeAsync<JsonElement>("SubscribeSpace", spaceId))
                    .GetProperty("ok").GetBoolean());
                var after = first.GetProperty("sequenceNo").GetString();
                var catchUp = await SendAsync(refreshedPage, HttpMethod.Get,
                    $"/api/v1/spaces/{spaceId}/messages?afterSequence={after}&limit=50", newToken);
                Assert.Equal(HttpStatusCode.OK, catchUp.StatusCode);
                var items = (await catchUp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items");
                Assert.Single(items.EnumerateArray());
                Assert.Equal(second.GetProperty("id").GetGuid(), items[0].GetProperty("id").GetGuid());
                Assert.Equal(HttpStatusCode.NoContent,
                    (await SendAsync(refreshedPage, HttpMethod.Post,
                        "/api/v1/auth/logout-all", newToken)).StatusCode);
                Assert.Equal(HttpStatusCode.Unauthorized,
                    (await SendAsync(refreshedPage, HttpMethod.Get,
                        $"/api/v1/spaces/{spaceId}/messages?limit=50", newToken)).StatusCode);
            }
        }
        finally { await CleanupAsync(factory, actors); }
    }

    [Fact]
    public async Task PostgreSql_constraints_reject_invalid_and_duplicate_messages_and_rollback_partial_writes()
    {
        using var factory = new SCDCWebApplicationFactory();
        using var client = factory.CreateClient();
        var actors = new List<TestActor>();
        try
        {
            var sender = await CreateActorAsync(client, "constraint_sender");
            var reader = await CreateActorAsync(client, "constraint_reader");
            actors.AddRange([sender, reader]);
            var dm = await SendAsync(client, HttpMethod.Post, "/api/v1/conversations/direct", sender.AccessToken,
                new { recipientUserId = reader.Id });
            var spaceId = (await dm.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
            await using var connection = new NpgsqlConnection(ConnectionString(factory));
            await connection.OpenAsync();

            await using (var invalid = await connection.BeginTransactionAsync())
            {
                await using var command = new NpgsqlCommand(
                    "INSERT INTO messaging.messages (space_id, author_user_id, client_message_id, message_type, content) "
                    + "VALUES (@space, @author, @key, 1, NULL)", connection, invalid);
                command.Parameters.AddWithValue("space", spaceId);
                command.Parameters.AddWithValue("author", sender.Id);
                command.Parameters.AddWithValue("key", Guid.NewGuid());
                var error = await Assert.ThrowsAsync<PostgresException>(async () => await command.ExecuteNonQueryAsync());
                Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
                Assert.Equal("ck_messages_text", error.ConstraintName);
                await invalid.RollbackAsync();
            }

            var transientId = Guid.CreateVersion7();
            var clientKey = Guid.NewGuid();
            await using (var transaction = await connection.BeginTransactionAsync())
            {
                await using var first = new NpgsqlCommand(
                    "INSERT INTO messaging.messages (id, space_id, author_user_id, client_message_id, message_type, content) "
                    + "VALUES (@id, @space, @author, @key, 1, 'rollback me')", connection, transaction);
                first.Parameters.AddWithValue("id", transientId);
                first.Parameters.AddWithValue("space", spaceId);
                first.Parameters.AddWithValue("author", sender.Id);
                first.Parameters.AddWithValue("key", clientKey);
                Assert.Equal(1, await first.ExecuteNonQueryAsync());
                await using var duplicate = new NpgsqlCommand(
                    "INSERT INTO messaging.messages (space_id, author_user_id, client_message_id, message_type, content) "
                    + "VALUES (@space, @author, @key, 1, 'duplicate')", connection, transaction);
                duplicate.Parameters.AddWithValue("space", spaceId);
                duplicate.Parameters.AddWithValue("author", sender.Id);
                duplicate.Parameters.AddWithValue("key", clientKey);
                var error = await Assert.ThrowsAsync<PostgresException>(async () => await duplicate.ExecuteNonQueryAsync());
                Assert.Equal(PostgresErrorCodes.UniqueViolation, error.SqlState);
                Assert.Equal("ux_messages_client_id", error.ConstraintName);
                await transaction.RollbackAsync();
            }
            await using var verify = new NpgsqlCommand(
                "SELECT count(*) FROM messaging.messages WHERE id = @id", connection);
            verify.Parameters.AddWithValue("id", transientId);
            Assert.Equal(0L, (long)(await verify.ExecuteScalarAsync())!);
            Assert.Empty((await (await SendAsync(client, HttpMethod.Get,
                $"/api/v1/spaces/{spaceId}/messages?limit=50", reader.AccessToken))
                .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray());
        }
        finally { await CleanupAsync(factory, actors); }
    }

    [Fact]
    public async Task Message_rate_limit_rejects_the_next_send_without_persisting_it()
    {
        using var factory = new SCDCWebApplicationFactory();
        using var client = factory.CreateClient();
        var actors = new List<TestActor>();
        try
        {
            var sender = await CreateActorAsync(client, "limit_sender");
            var reader = await CreateActorAsync(client, "limit_reader");
            actors.AddRange([sender, reader]);
            var dm = await SendAsync(client, HttpMethod.Post, "/api/v1/conversations/direct", sender.AccessToken,
                new { recipientUserId = reader.Id });
            var spaceId = (await dm.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
            var firstClientMessageId = Guid.NewGuid();
            for (var index = 0; index < 30; index++)
            {
                var sent = await SendAsync(client, HttpMethod.Post, $"/api/v1/spaces/{spaceId}/messages",
                    sender.AccessToken, new { clientMessageId = index == 0 ? firstClientMessageId : Guid.NewGuid(),
                        messageType = 1, content = $"message {index}" });
                Assert.Equal(HttpStatusCode.Created, sent.StatusCode);
            }
            var retry = await SendAsync(client, HttpMethod.Post, $"/api/v1/spaces/{spaceId}/messages",
                sender.AccessToken, new { clientMessageId = firstClientMessageId, messageType = 1, content = "message 0" });
            Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
            var rejected = await SendAsync(client, HttpMethod.Post, $"/api/v1/spaces/{spaceId}/messages",
                sender.AccessToken, new { clientMessageId = Guid.NewGuid(), messageType = 1, content = "blocked by rate limit" });
            Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
            Assert.Equal("60", rejected.Headers.RetryAfter?.Delta?.TotalSeconds.ToString("0")
                ?? rejected.Headers.GetValues("Retry-After").Single());
            await using var connection = new NpgsqlConnection(ConnectionString(factory));
            await connection.OpenAsync();
            await using var count = new NpgsqlCommand(
                "SELECT count(*) FROM messaging.messages WHERE space_id = @space", connection);
            count.Parameters.AddWithValue("space", spaceId);
            Assert.Equal(30L, (long)(await count.ExecuteScalarAsync())!);
        }
        finally { await CleanupAsync(factory, actors); }
    }

    [Fact]
    public async Task Default_request_logging_does_not_record_access_tokens_or_message_content()
    {
        using var logs = new TestLogCaptureProvider();
        using var factory = new SCDCWebApplicationFactory(useRealtimePublisher: false, additionalLogger: logs);
        using var client = factory.CreateClient();
        var actors = new List<TestActor>();
        try
        {
            var sender = await CreateActorAsync(client, "private_log_sender");
            var reader = await CreateActorAsync(client, "private_log_reader");
            actors.AddRange([sender, reader]);
            var dm = await SendAsync(client, HttpMethod.Post, "/api/v1/conversations/direct", sender.AccessToken,
                new { recipientUserId = reader.Id });
            var spaceId = (await dm.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
            var privateContent = $"private-chat-{Guid.NewGuid():N}";
            await SendTextAsync(client, sender, spaceId, privateContent);
            var output = string.Join('\n', logs.Entries);
            Assert.DoesNotContain(sender.AccessToken, output, StringComparison.Ordinal);
            Assert.DoesNotContain(sender.RefreshToken, output, StringComparison.Ordinal);
            Assert.DoesNotContain(privateContent, output, StringComparison.Ordinal);
        }
        finally { await CleanupAsync(factory, actors); }
    }

    private static async Task<TestActor> CreateActorAsync(HttpClient client, string label)
    {
        var username = $"release_{label}_{Guid.NewGuid():N}"[..30];
        const string password = "Messaging123";
        var registration = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { username, displayName = label, email = $"{username}@example.test", password });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        var body = await registration.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/auth/verify-email",
            new { token = body.GetProperty("developmentVerificationToken").GetString() })).StatusCode);
        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { login = username, password, deviceName = "Release coverage test" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var credentials = await login.Content.ReadFromJsonAsync<JsonElement>();
        return new(body.GetProperty("userId").GetGuid(), credentials.GetProperty("accessToken").GetString()!,
            credentials.GetProperty("refreshToken").GetString()!);
    }

    private static HubConnection CreateHub(SCDCWebApplicationFactory factory, HttpClient client, string token) =>
        new HubConnectionBuilder().WithUrl(new Uri(client.BaseAddress!, "/hubs/chat"), options =>
        {
            options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            options.Transports = HttpTransportType.LongPolling;
            options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
        }).Build();

    private static async Task<JsonElement> SendTextAsync(HttpClient client, TestActor actor, Guid spaceId, string content)
    {
        var response = await SendAsync(client, HttpMethod.Post, $"/api/v1/spaces/{spaceId}/messages",
            actor.AccessToken, new { clientMessageId = Guid.NewGuid(), messageType = 1, content });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method,
        string path, string token, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    private static string ConnectionString(SCDCWebApplicationFactory factory) =>
        factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("Database")!;

    private static async Task CleanupAsync(SCDCWebApplicationFactory factory, IReadOnlyCollection<TestActor> actors)
    {
        if (actors.Count == 0) return;
        var ids = actors.Select(actor => actor.Id).ToArray();
        await using var connection = new NpgsqlConnection(ConnectionString(factory));
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        foreach (var sql in new[]
        {
            "DELETE FROM integration.outbox_events WHERE space_id IN (SELECT id FROM messaging.spaces WHERE created_by_user_id = ANY(@ids))",
            "DELETE FROM messaging.messages WHERE space_id IN (SELECT id FROM messaging.spaces WHERE created_by_user_id = ANY(@ids))",
            "DELETE FROM messaging.space_user_states WHERE user_id = ANY(@ids)",
            "DELETE FROM messaging.direct_conversations WHERE user_low_id = ANY(@ids) OR user_high_id = ANY(@ids)",
            "DELETE FROM messaging.space_members WHERE user_id = ANY(@ids)",
            "DELETE FROM messaging.spaces WHERE created_by_user_id = ANY(@ids)",
            "DELETE FROM integration.outbox_events WHERE aggregate_id = ANY(@ids)",
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

    private sealed record TestActor(Guid Id, string AccessToken, string RefreshToken);
}
