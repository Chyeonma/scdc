using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SCDC.Api.Tests.Identity;
using SCDC.Modules.Messaging.Application;

namespace SCDC.Api.Tests.Messaging;

[Collection("DM database")]
public sealed class ConversationInboxTests(InboxFixture f) : IClassFixture<InboxFixture>
{
    [Fact]
    public async Task Default_pages_are_20_plus_5_unique_member_only_and_read_only()
    {
        var before = await f.D.Inner.MessagingCounts();
        var first = await f.Page(); var second = await f.Page(cursor: first.GetProperty("nextCursor").GetString());
        Assert.Equal(20, Ids(first).Length); Assert.Equal(5, Ids(second).Length);
        Assert.Equal(JsonValueKind.Null, second.GetProperty("nextCursor").ValueKind);
        var all = Ids(first).Concat(Ids(second)).ToArray(); Assert.Equal(25, all.Distinct().Count());
        Assert.Equal(f.Ids.Order().ToArray(), all);
        foreach (var item in first.GetProperty("items").EnumerateArray())
        {
            Assert.Equal(2, item.GetProperty("participants").GetArrayLength());
            Assert.Contains(item.GetProperty("participants").EnumerateArray(), p => p.GetProperty("id").GetGuid() == f.D.Inner.Actor.Id);
            Assert.Equal("0", item.GetProperty("lastSequence").GetString());
            Assert.Equal(JsonValueKind.Null, item.GetProperty("lastActivityAt").ValueKind);
            foreach (var person in item.GetProperty("participants").EnumerateArray())
                Assert.Equal(new[] { "displayName", "id", "username" }, person.EnumerateObject().Select(p => p.Name).Order());
        }
        var b = await f.Page(actor: f.D.Inner.Users["bao"]);
        Assert.Equal(new[] { f.ByPeer["bao"] }, Ids(b));
        Assert.Empty(Ids(await f.Page(actor: f.D.Inner.Users["editable"])));
        Assert.Equal(before, await f.D.Inner.MessagingCounts());
    }

    [Fact]
    public async Task Cursor_rejects_other_actor_limit_tamper_empty_and_other_purpose()
    {
        var cursor = (await f.Page()).GetProperty("nextCursor").GetString()!;
        foreach (var bad in new[] { "", "junk", cursor[..^8] + "invalid!", new string('x', 4097) })
            Assert.Equal("CURSOR_INVALID", (await f.Page(cursor: bad, expected: 400)).GetProperty("errorCode").GetString());
        await f.Page(cursor: cursor, actor: f.D.Inner.Users["bao"], expected: 400);
        await f.Page(cursor: cursor, limit: 10, expected: 400);
        var foreign = f.D.Inner.Factory.Services.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("Identity.UserSearch.v1").Protect("foreign cursor");
        await f.Page(cursor: foreign, expected: 400);
        foreach (var limit in new[] { 0, 51 }) await f.Page(limit: limit, expected: 400);
        Assert.Single(Ids(await f.Page(limit: 1)));
        Assert.Equal(25, Ids(await f.Page(limit: 50)).Length);
    }

    [Fact]
    public async Task Cursor_survives_new_host_same_keys_and_expires_after_24_hours()
    {
        var cursor = (await f.Page()).GetProperty("nextCursor").GetString()!;
        using var host = f.D.Inner.MakeFactory(); using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/direct-conversations?cursor=" + Uri.EscapeDataString(cursor));
        request.Headers.Authorization = new("Bearer", f.D.Inner.Actor.AccessToken);
        using var result = await client.SendAsync(request); Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(5, Ids(await result.Content.ReadFromJsonAsync<JsonElement>()).Length);
        f.D.Inner.Clock.CursorFuture = true;
        try { await f.Page(cursor: cursor, expected: 400); }
        finally { f.D.Inner.Clock.CursorFuture = false; }
    }

    [Fact]
    public async Task Keyset_handles_tied_activity_then_null_tail_without_duplicates()
    {
        // Synthetic projection ordering only; this does not prove committed message activity.
        foreach (var id in f.Ids.Take(4))
            await f.D.Sql("UPDATE messaging.spaces SET last_activity_at='2026-10-09T01:00:00Z' WHERE id=@id", id);
        try
        {
            var all = new List<Guid>(); string? cursor = null;
            do { var page = await f.Page(limit: 3, cursor: cursor); all.AddRange(Ids(page)); cursor = page.GetProperty("nextCursor").GetString(); } while (cursor is not null);
            Assert.Equal(f.Ids.Take(4).Order().Concat(f.Ids.Skip(4).Order()), all);
        }
        finally { foreach (var id in f.Ids.Take(4)) await f.D.Sql("UPDATE messaging.spaces SET last_activity_at=NULL WHERE id=@id", id); }
    }

    [Fact]
    public async Task Deleted_space_is_hidden_on_next_page_and_unavailable_peer_stays_public()
    {
        var cursor = (await f.Page()).GetProperty("nextCursor").GetString()!;
        var last = f.Ids.Order().Last();
        await f.D.Sql("UPDATE messaging.spaces SET status=3, deleted_at=now() WHERE id=@id", last);
        await f.D.Sql("UPDATE identity.users SET status=3 WHERE id=@id", f.D.Inner.Users["bao"].Id);
        try
        {
            Assert.DoesNotContain(last, Ids(await f.Page(cursor: cursor)));
            var item = (await f.Page(limit: 50)).GetProperty("items").EnumerateArray().Single(i => i.GetProperty("id").GetGuid() == f.ByPeer["bao"]);
            var peer = item.GetProperty("participants").EnumerateArray().Single(p => p.GetProperty("id").GetGuid() == f.D.Inner.Users["bao"].Id);
            Assert.Equal("unavailable", peer.GetProperty("availability").GetString());
            Assert.False(peer.TryGetProperty("email", out _));
            await f.Page(actor: f.D.Inner.Users["bao"], expected: 401);
        }
        finally
        {
            await f.D.Sql("UPDATE messaging.spaces SET status=1, deleted_at=NULL WHERE id=@id", last);
            await f.D.Sql("UPDATE identity.users SET status=1 WHERE id=@id", f.D.Inner.Users["bao"].Id);
        }
    }

    [Fact]
    public async Task Anonymous_revoked_and_contract_wrong_session_or_stamp_are_rejected()
    {
        using var anonymous = await f.D.Inner.Client.GetAsync("/api/v1/direct-conversations");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        var actor = await f.D.Inner.Login(f.D.Inner.Actor.Username);
        using var logout = await f.D.Inner.Send("/api/v1/auth/logout", actor, new { refreshToken = actor.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        await f.Page(actor: actor, expected: 401);
        var active = f.D.Inner.Actor;
        foreach (var bad in new[] {
            new InboxRequest(active.Id, Guid.NewGuid(), active.SecurityStamp),
            new InboxRequest(active.Id, active.SessionId, Guid.NewGuid()) })
        {
            await using var scope = f.D.Inner.Factory.Services.CreateAsyncScope();
            var result = await scope.ServiceProvider.GetRequiredService<IConversationInbox>().ListAsync(bad, default);
            Assert.True(result.IsFailure); Assert.Equal("Common.Unauthorized", result.Error.Code);
        }
    }

    [Fact]
    public async Task Authority_failure_in_auth_middleware_is_503_without_mock_inbox()
    {
        using var host = f.D.Inner.Factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> {
                ["ConnectionStrings:Database"] = "Host=127.0.0.1;Port=1;Database=scdc_dm_acceptance_test;Username=scdc_dm_test;Password=unused;Timeout=1" })));
        using var client = host.CreateClient(); using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/direct-conversations");
        request.Headers.Authorization = new("Bearer", f.D.Inner.Actor.AccessToken);
        using var response = await client.SendAsync(request); Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal("AUTHORITY_UNAVAILABLE", body.GetProperty("errorCode").GetString());
        Assert.False(body.TryGetProperty("items", out _));
    }
    private static Guid[] Ids(JsonElement page) => UserSearchFixture.Ids(page);
}

public sealed class InboxFixture : IAsyncLifetime
{
    public DirectFixture D { get; } = new();
    public Dictionary<string, Guid> ByPeer { get; } = [];
    public Guid[] Ids => ByPeer.Values.ToArray();
    public async Task InitializeAsync()
    {
        await D.InitializeAsync();
        foreach (var alias in new[] { "bao", "chi" }.Concat(Enumerable.Range(1, 23).Select(i => $"search{i:00}")))
            ByPeer[alias] = (await D.Open(D.Inner.Actor, D.Inner.Users[alias].Id)).GetProperty("id").GetGuid();
    }
    public async Task<JsonElement> Page(int limit = 20, string? cursor = null, UserSearchFixture.ActorData? actor = null, int expected = 200)
    {
        var path = $"/api/v1/direct-conversations?limit={limit}";
        if (cursor is not null) path += "&cursor=" + Uri.EscapeDataString(cursor);
        using var response = await D.Inner.Send(path, actor); Assert.Equal(expected, (int)response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
    public Task DisposeAsync() => D.DisposeAsync();
}
