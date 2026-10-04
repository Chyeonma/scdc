using System.Diagnostics;
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

// Isolated TestServer benchmark. Run only with the dedicated PostgreSQL test fixture.
using var factory = SCDCWebApplicationFactory.ForPerformance(Path.GetFullPath("services/SCDC.Api"));
using var client = factory.CreateClient();
var connectionString = factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("Database")!;
var database = new NpgsqlConnectionStringBuilder(connectionString);
if (database.Database != "scdc_chat_test")
    throw new InvalidOperationException("MessagingPerf only runs against scdc_chat_test.");

var actors = new List<Actor>();
try
{
    var sender = await CreateActorAsync("perfsender");
    var reader = await CreateActorAsync("perfreader");
    var dm = await SendAsync(HttpMethod.Post, "/api/v1/conversations/direct", sender.Token,
        new { recipientUserId = reader.Id });
    await RequireAsync(dm, HttpStatusCode.Created);
    var spaceId = (await dm.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    await SeedHistoryAsync(spaceId, sender.Id, 1_000);

    var timings = new Dictionary<string, List<double>>
    {
        ["send"] = [], ["history"] = [], ["search"] = [], ["unread"] = []
    };
    var duplicateClientMessageId = Guid.NewGuid();
    var sendStarted = Stopwatch.GetTimestamp();
    for (var index = 0; index < 20; index++)
    {
        await MeasureAsync("send", HttpMethod.Post, $"/api/v1/spaces/{spaceId}/messages", sender.Token,
            new { clientMessageId = index == 0 ? duplicateClientMessageId : Guid.NewGuid(),
                messageType = 1, content = $"perf sent {index}" },
            HttpStatusCode.Created);
    }
    var sendThroughput = 20 / Stopwatch.GetElapsedTime(sendStarted).TotalSeconds;
    using (var duplicate = await SendAsync(HttpMethod.Post, $"/api/v1/spaces/{spaceId}/messages",
        sender.Token, new { clientMessageId = duplicateClientMessageId, messageType = 1, content = "perf sent 0" }))
        await RequireAsync(duplicate, HttpStatusCode.OK);
    for (var index = 0; index < 40; index++)
    {
        await MeasureAsync("history", HttpMethod.Get,
            $"/api/v1/spaces/{spaceId}/messages?limit=50", reader.Token, null, HttpStatusCode.OK);
        await MeasureAsync("search", HttpMethod.Get,
            $"/api/v1/spaces/{spaceId}/messages/search?q=perfneedle&limit=20", reader.Token, null,
            HttpStatusCode.OK);
        await MeasureAsync("unread", HttpMethod.Get,
            "/api/v1/spaces?limit=50", reader.Token, null, HttpStatusCode.OK);
    }

    var plans = new Dictionary<string, object>();
    plans["history"] = await ExplainAsync("""
        SELECT id FROM messaging.messages WHERE space_id = @space
        ORDER BY sequence_no DESC LIMIT 50
        """, spaceId);
    plans["search"] = await ExplainAsync("""
        SELECT id FROM messaging.messages WHERE space_id = @space AND deleted_at IS NULL
          AND search_vector @@ plainto_tsquery('simple', 'perfneedle')
        ORDER BY sequence_no DESC LIMIT 21
        """, spaceId);
    plans["outbox_claim"] = await ExplainAsync("""
        SELECT id FROM integration.outbox_events
        WHERE published_at IS NULL AND available_at <= clock_timestamp()
          AND attempt_count < 10 AND event_type LIKE 'Messaging.%'
        ORDER BY available_at, occurred_at, id LIMIT 1
        """, spaceId);
    var liveConnections = Enumerable.Range(0, 50).Select(_ =>
        new HubConnectionBuilder().WithUrl(new Uri(client.BaseAddress!, "/hubs/chat"), options =>
        {
            options.AccessTokenProvider = () => Task.FromResult<string?>(reader.Token);
            options.Transports = HttpTransportType.LongPolling;
            options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
        }).Build()).ToArray();
    var connectStarted = Stopwatch.GetTimestamp();
    try
    {
        await Task.WhenAll(liveConnections.Select(connection => connection.StartAsync()));
        using var scope = factory.Services.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IMessagingOutboxDispatcher>();
        while (await dispatcher.DispatchDueAsync(CancellationToken.None) > 0) { }
    }
    finally
    {
        await Task.WhenAll(liveConnections.Select(async connection =>
        {
            await connection.StopAsync();
            await connection.DisposeAsync();
        }));
    }
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    await using var lagCommand = new NpgsqlCommand("""
        SELECT count(*), coalesce(max(extract(epoch FROM (published_at - occurred_at))), 0),
               count(*) FILTER (WHERE published_at IS NULL)
        FROM integration.outbox_events WHERE space_id = @space
        """, connection);
    lagCommand.Parameters.AddWithValue("space", spaceId);
    await using var lag = await lagCommand.ExecuteReaderAsync();
    await lag.ReadAsync();
    var result = new
    {
        environment = "Windows local / .NET 10 TestServer / PostgreSQL 18 Docker / single API process",
        historyRows = 1_000,
        sampleCount = timings.ToDictionary(item => item.Key, item => item.Value.Count),
        sendThroughputPerSecond = Math.Round(sendThroughput, 1),
        concurrentConnections = liveConnections.Length,
        connectionAndDispatchSeconds = Math.Round(Stopwatch.GetElapsedTime(connectStarted).TotalSeconds, 2),
        p95Milliseconds = timings.ToDictionary(item => item.Key, item => Math.Round(Percentile(item.Value, 0.95), 1)),
        maxMilliseconds = timings.ToDictionary(item => item.Key, item => Math.Round(item.Value.Max(), 1)),
        outboxEvents = lag.GetInt64(0),
        outboxMaxLagSeconds = Math.Round(lag.GetDouble(1), 3),
        outboxUnpublished = lag.GetInt64(2),
        plans
    };
    Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));

    async Task MeasureAsync(string operation, HttpMethod method, string path, string token, object? body,
        HttpStatusCode expected)
    {
        var started = Stopwatch.GetTimestamp();
        using var response = await SendAsync(method, path, token, body);
        await RequireAsync(response, expected);
        await response.Content.LoadIntoBufferAsync();
        timings[operation].Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
    }
}
finally
{
    await CleanupAsync(actors);
}

async Task<Actor> CreateActorAsync(string label)
{
    var username = $"perf_{label}_{Guid.NewGuid():N}"[..30];
    const string password = "Messaging123";
    using var registration = await client.PostAsJsonAsync("/api/v1/auth/register",
        new { username, displayName = label, email = $"{username}@example.test", password });
    await RequireAsync(registration, HttpStatusCode.Created);
    var body = await registration.Content.ReadFromJsonAsync<JsonElement>();
    var userId = body.GetProperty("userId").GetGuid();
    actors.Add(new Actor(userId, string.Empty));
    using var verification = await client.PostAsJsonAsync("/api/v1/auth/verify-email",
        new { token = body.GetProperty("developmentVerificationToken").GetString() });
    await RequireAsync(verification, HttpStatusCode.NoContent);
    using var login = await client.PostAsJsonAsync("/api/v1/auth/login",
        new { login = username, password, deviceName = "Messaging performance fixture" });
    await RequireAsync(login, HttpStatusCode.OK);
    var tokens = await login.Content.ReadFromJsonAsync<JsonElement>();
    var actor = new Actor(userId, tokens.GetProperty("accessToken").GetString()!);
    actors[^1] = actor;
    return actor;
}

async Task SeedHistoryAsync(Guid spaceId, Guid senderId, int count)
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    await using var transaction = await connection.BeginTransactionAsync();
    await using var seed = new NpgsqlCommand("""
        INSERT INTO messaging.messages
            (id, space_id, author_user_id, client_message_id, message_type, content, created_at)
        SELECT uuidv7(), @space, @sender, uuidv7(), 1,
               CASE WHEN number % 10 = 0 THEN 'perfneedle history row '
                    ELSE 'history row ' END || number, clock_timestamp()
        FROM generate_series(1, @count) AS number;
        UPDATE messaging.spaces AS s SET last_message_id = latest.id,
            last_message_sequence = latest.sequence_no, last_activity_at = clock_timestamp()
        FROM (SELECT id, sequence_no FROM messaging.messages
              WHERE space_id = @space ORDER BY sequence_no DESC LIMIT 1) AS latest
        WHERE s.id = @space;
        """, connection, transaction);
    seed.Parameters.AddWithValue("space", spaceId);
    seed.Parameters.AddWithValue("sender", senderId);
    seed.Parameters.AddWithValue("count", count);
    await seed.ExecuteNonQueryAsync();
    await transaction.CommitAsync();
}

async Task<object> ExplainAsync(string sql, Guid spaceId)
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    await using var command = new NpgsqlCommand("EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON) " + sql, connection);
    command.Parameters.AddWithValue("space", spaceId);
    using var document = JsonDocument.Parse((string)(await command.ExecuteScalarAsync())!);
    var root = document.RootElement[0];
    var indexes = new HashSet<string>(StringComparer.Ordinal);
    CollectIndexes(root.GetProperty("Plan"), indexes);
    return new { actualMilliseconds = Math.Round(root.GetProperty("Execution Time").GetDouble(), 3),
        indexes = indexes.Order().ToArray(), rootNode = root.GetProperty("Plan").GetProperty("Node Type").GetString() };
}

static void CollectIndexes(JsonElement node, HashSet<string> indexes)
{
    if (node.TryGetProperty("Index Name", out var name)) indexes.Add(name.GetString()!);
    if (node.TryGetProperty("Plans", out var children))
        foreach (var child in children.EnumerateArray()) CollectIndexes(child, indexes);
}

async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string token, object? body = null)
{
    using var request = new HttpRequestMessage(method, path);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    if (body is not null) request.Content = JsonContent.Create(body);
    return await client.SendAsync(request);
}

static async Task RequireAsync(HttpResponseMessage response, HttpStatusCode expected)
{
    if (response.StatusCode != expected)
        throw new InvalidOperationException($"Expected {(int)expected}, got {(int)response.StatusCode}: "
            + await response.Content.ReadAsStringAsync());
}

async Task CleanupAsync(IReadOnlyCollection<Actor> created)
{
    if (created.Count == 0) return;
    var ids = created.Select(actor => actor.Id).ToArray();
    await using var connection = new NpgsqlConnection(connectionString);
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

static double Percentile(List<double> measurements, double fraction)
{
    var sorted = measurements.Order().ToArray();
    return sorted[Math.Clamp((int)Math.Ceiling(sorted.Length * fraction) - 1, 0, sorted.Length - 1)];
}

internal sealed record Actor(Guid Id, string Token);
