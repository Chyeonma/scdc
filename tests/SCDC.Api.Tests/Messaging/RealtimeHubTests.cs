using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using SCDC.Modules.Messaging.Realtime;

namespace SCDC.Api.Tests.Messaging;

[Collection("DM database")]
public sealed class RealtimeHubTests(TextMessageFixture f) : IClassFixture<TextMessageFixture>
{
    private OutboxDispatcher Dispatcher => f.D.Inner.Factory.Services.GetRequiredService<OutboxDispatcher>();

    [Fact]
    public async Task Authenticated_websocket_repeat_subscribe_routes_current_DTO_once_and_outsider_is_private()
    {
        var space = await f.Open("bao");
        await using var a = await SocketClient.Connect(f, f.D.Inner.Actor.AccessToken);
        await using var b = await SocketClient.Connect(f, f.D.Inner.Users["bao"].AccessToken);
        await using var c = await SocketClient.Connect(f, f.D.Inner.Users["chi"].AccessToken);
        foreach (var socket in new[] { a, b })
        {
            await socket.Subscribe(space); await socket.Subscribe(space);
        }
        var denied = await c.Invoke("SubscribeConversation", space);
        var error = denied.GetProperty("error").GetString()!;
        var problem = JsonDocument.Parse(error[error.IndexOf('{')..(error.LastIndexOf('}') + 1)]).RootElement;
        Assert.Equal(404, problem.GetProperty("status").GetInt32()); Assert.Equal("RESOURCE_NOT_FOUND", problem.GetProperty("errorCode").GetString());
        Assert.DoesNotContain(f.D.Inner.Actor.Id.ToString(), denied.ToString()); Assert.DoesNotContain(f.D.Inner.Users["bao"].Id.ToString(), denied.ToString());
        var sent = await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), "WS-current");
        Assert.True(await Dispatcher.DispatchOnceAsync(conversationId: space));
        var left = await a.Message(); var right = await b.Message();
        Assert.Equal(sent.GetProperty("id").GetGuid(), right.GetProperty("message").GetProperty("id").GetGuid());
        Assert.Equal(left.GetProperty("eventId").GetGuid(), right.GetProperty("eventId").GetGuid());
        Assert.Equal("1", right.GetProperty("message").GetProperty("version").GetString());
        await b.Subscribe(space); Assert.Empty(b.Events); Assert.Empty(c.Events);
        Assert.Equal("1:1:1:1", await f.Counts(space));
        Assert.Equal("1:1:0:0", await f.Scalar("SELECT concat(count(*),':',sum(attempt_count),':',count(*) FILTER(WHERE published_at IS NULL),':',count(*) FILTER(WHERE payload ? 'content' OR payload ? 'body')) FROM integration.outbox_events WHERE space_id=@id", space));
        // Header is required on REST. A valid query token must not authorize a history read.
        using var queryOnly = await f.D.Inner.Client.GetAsync($"/api/v1/direct-conversations/{space}/messages?access_token={Uri.EscapeDataString(f.D.Inner.Actor.AccessToken)}");
        Assert.Equal(HttpStatusCode.Unauthorized, queryOnly.StatusCode);
    }

    [Fact]
    public async Task Unsubscribe_ack_and_another_subscription_prevent_late_room_delivery()
    {
        var space = await f.Open("search01"); var other = (await f.D.Open(f.D.Inner.Users["search01"], f.D.Inner.Users["chi"].Id)).GetProperty("id").GetGuid();
        await using var a = await SocketClient.Connect(f, f.D.Inner.Actor.AccessToken);
        await using var b = await SocketClient.Connect(f, f.D.Inner.Users["search01"].AccessToken);
        await a.Subscribe(space); await b.Subscribe(space);
        var ack = await b.Invoke("UnsubscribeConversation", space); Assert.False(ack.GetProperty("result").GetProperty("subscribed").GetBoolean());
        await b.Subscribe(other);
        await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), "ROUTE-DENY"); Assert.True(await Dispatcher.DispatchOnceAsync(conversationId: space));
        await a.Message(); await b.Subscribe(other); Assert.Empty(b.Events);
        Assert.Equal("1:1:1:1", await f.Counts(space));
        await b.Subscribe(space); // Authorized history remains available even though the live event was not routed.
        using var history = await f.D.Inner.Send($"/api/v1/direct-conversations/{space}/messages", f.D.Inner.Users["search01"]);
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
    }

    [Fact]
    public async Task Dispatch_reads_edited_current_state_and_real_republish_is_idempotent_at_the_writer()
    {
        var space = await f.Open("search02"); await using var b = await SocketClient.Connect(f, f.D.Inner.Users["search02"].AccessToken); await b.Subscribe(space);
        var sent = await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), "original-not-outbox");
        await f.D.Sql("UPDATE messaging.messages SET content='current-only',version=2,edited_at=now() WHERE space_id=@id", space);
        Assert.True(await Dispatcher.DispatchOnceAsync(conversationId: space)); var e = await b.Message();
        Assert.Equal("current-only", e.GetProperty("message").GetProperty("content").GetString()); Assert.Equal("2", e.GetProperty("message").GetProperty("version").GetString());
        await f.D.Sql("UPDATE integration.outbox_events SET published_at=NULL,available_at=clock_timestamp() WHERE space_id=@id", space);
        Assert.True(await Dispatcher.DispatchOnceAsync(conversationId: space)); var repeated = await b.Message();
        Assert.Equal(e.GetProperty("eventId").GetGuid(), repeated.GetProperty("eventId").GetGuid());
        Assert.Equal(sent.GetProperty("id").GetGuid(), repeated.GetProperty("message").GetProperty("id").GetGuid());
        Assert.Equal("1:1:1:1", await f.Counts(space));
        Assert.Equal("0", await f.Scalar("SELECT count(*)::text FROM integration.outbox_events WHERE space_id=@id AND (payload ? 'content' OR payload ? 'body')", space));
    }

    [Fact]
    public async Task Pending_unexpired_lease_is_not_stolen_expired_lease_recovers_and_rollback_never_publishes()
    {
        var space = await f.Open("search03"); await using var b = await SocketClient.Connect(f, f.D.Inner.Users["search03"].AccessToken); await b.Subscribe(space);
        await f.Fault(space, true);
        try { await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), "rollback-no-event", 503); }
        finally { await f.Fault(space, false); }
        Assert.False(await Dispatcher.DispatchOnceAsync(conversationId: space)); await b.Subscribe(space); Assert.Empty(b.Events);
        await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), "lease-recovery");
        await f.D.Sql("UPDATE integration.outbox_events SET lease_token=gen_random_uuid(),lease_expires_at=clock_timestamp()+interval '1 minute' WHERE space_id=@id", space);
        Assert.False(await Dispatcher.DispatchOnceAsync(conversationId: space));
        await f.D.Sql("UPDATE integration.outbox_events SET lease_expires_at=clock_timestamp()-interval '1 second' WHERE space_id=@id", space);
        Assert.True(await Dispatcher.DispatchOnceAsync(conversationId: space)); await b.Message();
        Assert.Equal("1:1:1:1", await f.Counts(space));
        Assert.Equal("0", await f.Scalar("SELECT count(*)::text FROM integration.outbox_events WHERE space_id=@id AND (published_at IS NULL OR lease_token IS NOT NULL)", space));
    }

    [Fact]
    public async Task Publish_mark_failure_keeps_body_free_retry_with_backoff_and_republishes_without_new_send()
    {
        var space = await f.Open("search05"); await using var b = await SocketClient.Connect(f, f.D.Inner.Users["search05"].AccessToken); await b.Subscribe(space);
        await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), "publish-marker-fault");
        var trigger = f.D.Inner.Prefix + "publish_fault";
        await using var db = await f.D.Connect();
        await using (var install = new Npgsql.NpgsqlCommand($"""
            CREATE FUNCTION integration.{trigger}() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN IF NEW.space_id='{space}' AND NEW.published_at IS NOT NULL THEN
             RAISE EXCEPTION USING ERRCODE='58000',MESSAGE='Owned publish marker fault'; END IF; RETURN NEW; END $$;
            CREATE TRIGGER {trigger} BEFORE UPDATE ON integration.outbox_events FOR EACH ROW EXECUTE FUNCTION integration.{trigger}();
            """, db)) await install.ExecuteNonQueryAsync();
        try
        {
            Assert.True(await Dispatcher.DispatchOnceAsync(conversationId: space)); var first = await b.Message();
            Assert.Equal("1", await f.Scalar("SELECT count(*)::text FROM integration.outbox_events WHERE space_id=@id AND published_at IS NULL AND lease_token IS NULL AND last_error='DISPATCH_UNAVAILABLE' AND available_at>occurred_at AND attempt_count=1", space));
            Assert.False(await Dispatcher.DispatchOnceAsync(conversationId: space));
            await using (var remove = new Npgsql.NpgsqlCommand($"DROP TRIGGER {trigger} ON integration.outbox_events; DROP FUNCTION integration.{trigger}();", db)) await remove.ExecuteNonQueryAsync();
            await f.D.Sql("UPDATE integration.outbox_events SET available_at=clock_timestamp() WHERE space_id=@id", space);
            Assert.True(await Dispatcher.DispatchOnceAsync(conversationId: space)); var second = await b.Message();
            Assert.Equal(first.GetProperty("eventId").GetGuid(), second.GetProperty("eventId").GetGuid()); Assert.Equal("1:1:1:1", await f.Counts(space));
        }
        finally
        {
            await using var remove = new Npgsql.NpgsqlCommand($"DROP TRIGGER IF EXISTS {trigger} ON integration.outbox_events; DROP FUNCTION IF EXISTS integration.{trigger}();", db);
            await remove.ExecuteNonQueryAsync();
        }
    }

    [Fact]
    public async Task Revoked_existing_session_cannot_receive_content_or_subscribe_using_the_old_token()
    {
        var space = await f.Open("search04"); var actor = f.D.Inner.Users["search04"];
        await using var b = await SocketClient.Connect(f, actor.AccessToken); await b.Subscribe(space);
        await f.D.Sql("UPDATE identity.auth_sessions SET revoked_at=clock_timestamp() WHERE id=@id", actor.SessionId);
        await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), "private-after-revoke"); await Dispatcher.DispatchOnceAsync(conversationId: space);
        var revoked = await b.Revoked(); Assert.Equal("session", revoked.GetProperty("scope").GetString()); Assert.Empty(b.Events);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/hubs/chat/negotiate?negotiateVersion=1");
        request.Headers.Authorization = new("Bearer", actor.AccessToken);
        using var response = await f.D.Inner.Client.SendAsync(request); Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("logoutAll")]
    [InlineData("stamp")]
    [InlineData("expired")]
    [InlineData("disabled")]
    [InlineData("unverified")]
    public async Task Existing_connection_cutoff_reconciles_without_a_new_writer_and_old_token_cannot_reconnect(string mode)
    {
        var space = await f.Open("search15"); var actor = await f.D.Inner.Login(f.D.Inner.Users["search15"].Username);
        await using var b = await SocketClient.Connect(f, actor.AccessToken); await b.Subscribe(space);
        await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), "committed-before-cutoff");
        try
        {
            if(mode == "logoutAll")
            {
                using var request = new HttpRequestMessage(HttpMethod.Post,"/api/v1/auth/logout-all"); request.Headers.Authorization=new("Bearer",actor.AccessToken);
                using var response=await f.D.Inner.Client.SendAsync(request);Assert.Equal(HttpStatusCode.NoContent,response.StatusCode);
            }
            else if(mode=="stamp")await f.D.Sql("UPDATE identity.user_security_states SET security_stamp=gen_random_uuid() WHERE user_id=@id",actor.Id);
            else if(mode=="expired")await f.D.Sql("UPDATE identity.auth_sessions SET expires_at=clock_timestamp() WHERE id=@id",actor.SessionId);
            else if(mode=="disabled")await f.D.Sql("UPDATE identity.users SET status=3 WHERE id=@id",actor.Id);
            else await f.D.Sql("UPDATE identity.user_emails SET verified_at=NULL WHERE user_id=@id AND is_primary",actor.Id);
            var committed=DateTimeOffset.UtcNow; var revoked=await b.Revoked(); Assert.True(DateTimeOffset.UtcNow-committed < TimeSpan.FromSeconds(5));
            await Dispatcher.DispatchOnceAsync(conversationId:space);Assert.Empty(b.Events);
            using var negotiation=new HttpRequestMessage(HttpMethod.Post,"/hubs/chat/negotiate?negotiateVersion=1");negotiation.Headers.Authorization=new("Bearer",actor.AccessToken);
            using var forbidden=await f.D.Inner.Client.SendAsync(negotiation);Assert.Equal(HttpStatusCode.Unauthorized,forbidden.StatusCode);
        }
        finally
        {
            if(mode=="disabled")await f.D.Sql("UPDATE identity.users SET status=1 WHERE id=@id",actor.Id);
            if(mode=="unverified")await f.D.Sql("UPDATE identity.user_emails SET verified_at=clock_timestamp() WHERE user_id=@id AND is_primary",actor.Id);
        }
    }

    private sealed class SocketClient : IAsyncDisposable
    {
        private readonly WebSocket socket;
        private readonly CancellationTokenSource stop = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> pending = new();
        private readonly Channel<JsonElement> messages = Channel.CreateUnbounded<JsonElement>();
        private readonly Channel<JsonElement> revocations = Channel.CreateUnbounded<JsonElement>();
        private readonly TaskCompletionSource handshake = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Task pump; private int serial;
        public ConcurrentQueue<JsonElement> Events { get; } = new();
        private SocketClient(WebSocket socket) { this.socket = socket; pump = Pump(); }
        public static async Task<SocketClient> Connect(TextMessageFixture fixture, string token)
        {
            var transport = fixture.D.Inner.Factory.Server.CreateWebSocketClient();
            var socket = await transport.ConnectAsync(new Uri("ws://localhost/hubs/chat?access_token=" + Uri.EscapeDataString(token)), default);
            var client = new SocketClient(socket); await client.Send(new { protocol = "json", version = 1 });
            await client.handshake.Task.WaitAsync(TimeSpan.FromSeconds(5)); return client;
        }
        private async Task Send(object frame) => await socket.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(frame) + "\u001e"), WebSocketMessageType.Text, true, stop.Token);
        public async Task<JsonElement> Invoke(string method, Guid space)
        {
            var id = Interlocked.Increment(ref serial).ToString(); var done = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
            pending[id] = done; await Send(new { type = 1, invocationId = id, target = method, arguments = new[] { new { conversationId = space } } });
            return await done.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        public async Task Subscribe(Guid space)
        {
            var ack = await Invoke("SubscribeConversation", space); Assert.False(ack.TryGetProperty("error", out _));
            Assert.True(ack.GetProperty("result").GetProperty("subscribed").GetBoolean());
        }
        public async Task<JsonElement> Message()
        {
            var item = await messages.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)); Events.TryDequeue(out _); return item;
        }
        public async Task<JsonElement> Revoked() => await revocations.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        private async Task Pump()
        {
            var buffer = new byte[16384]; using var data = new MemoryStream();
            try
            {
                while (!stop.IsCancellationRequested)
                {
                    var received = await socket.ReceiveAsync(buffer, stop.Token);
                    if (received.MessageType == WebSocketMessageType.Close) return;
                    data.Write(buffer, 0, received.Count); if (!received.EndOfMessage) continue;
                    var text = Encoding.UTF8.GetString(data.ToArray()); data.SetLength(0);
                    foreach (var frame in text.Split('\u001e', StringSplitOptions.RemoveEmptyEntries))
                    {
                        using var doc = JsonDocument.Parse(frame); var root = doc.RootElement;
                        if (!root.TryGetProperty("type", out var kind)) { handshake.TrySetResult(); continue; }
                        if (kind.GetInt32() == 3 && pending.TryRemove(root.GetProperty("invocationId").GetString()!, out var completion)) completion.TrySetResult(root.Clone());
                        if (kind.GetInt32() != 1) continue;
                        var payload = root.GetProperty("arguments")[0].Clone();
                        if (root.GetProperty("target").GetString() == "MessageChanged") { Events.Enqueue(payload); await messages.Writer.WriteAsync(payload); }
                        if (root.GetProperty("target").GetString() == "AccessRevoked") await revocations.Writer.WriteAsync(payload);
                    }
                }
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
            catch (WebSocketException) when (stop.IsCancellationRequested) { }
            catch (ObjectDisposedException) when (stop.IsCancellationRequested) { }
        }
        public async ValueTask DisposeAsync() { stop.Cancel(); socket.Abort(); socket.Dispose(); await pump; stop.Dispose(); }
    }
}
