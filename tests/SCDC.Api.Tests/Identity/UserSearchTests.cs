using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Microsoft.AspNetCore.WebUtilities;
using SCDC.Contracts.Identity;
using SCDC.Api.Tests.Infrastructure;

namespace SCDC.Api.Tests.Identity;

[Collection("DM database")]
public sealed class UserSearchTests(UserSearchFixture fixture) : IClassFixture<UserSearchFixture>
{
    [Fact]
    public async Task Search_projects_public_fields_and_filters_self_pending_disabled_deleted_and_unverified()
    {
        var body = await fixture.Search(fixture.Prefix);
        var ids = body.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ToArray();
        Assert.DoesNotContain(fixture.Actor.Id, ids);
        foreach (var alias in new[] { "pending", "disabled", "deleted", "unverified" })
            Assert.DoesNotContain(fixture.Users[alias].Id, ids);
        foreach (var item in body.GetProperty("items").EnumerateArray())
            Assert.Equal(new[] { "displayName", "id", "username" }, item.EnumerateObject().Select(x => x.Name).Order().ToArray());
    }

    [Fact]
    public async Task Search_is_case_insensitive_accent_sensitive_and_NFC_equivalent()
    {
        var q = "Bảo " + fixture.Prefix;
        var lower = await fixture.Search(q);
        var upper = await fixture.Search(q.ToUpperInvariant());
        var decomposed = await fixture.Search(q.Normalize(NormalizationForm.FormD));
        Assert.Equal(UserSearchFixture.Ids(lower), UserSearchFixture.Ids(upper));
        Assert.Equal(UserSearchFixture.Ids(lower), UserSearchFixture.Ids(decomposed));
        Assert.Contains(fixture.Users["bao"].Id, UserSearchFixture.Ids(lower));
        Assert.Contains(fixture.Users["chi"].Id, UserSearchFixture.Ids(lower));
        var noAccent = await fixture.Search("Bao " + fixture.Prefix);
        Assert.Empty(UserSearchFixture.Ids(noAccent));
    }

    [Fact]
    public async Task Literal_percent_underscore_backslash_are_not_SQL_wildcards()
    {
        foreach (var q in new[] { "P%", "%_", "_\\" })
        {
            var body = await fixture.Search(q);
            Assert.Contains(fixture.Users["literal"].Id, UserSearchFixture.Ids(body));
            Assert.DoesNotContain(fixture.Users["decoy"].Id, UserSearchFixture.Ids(body));
        }
    }

    [Fact]
    public async Task Exact_username_ranks_first_and_cursor_continues_rank_boundary()
    {
        var username = fixture.Users["bao"].Username;
        var first = await fixture.Search(username.ToUpperInvariant(), 1);
        Assert.Equal(fixture.Users["bao"].Id, UserSearchFixture.Ids(first).Single());
        var next = await fixture.Search(username, 1, first.GetProperty("nextCursor").GetString());
        Assert.Equal(fixture.Users["exact-display"].Id, UserSearchFixture.Ids(next).Single());
        Assert.Equal(JsonValueKind.Null, next.GetProperty("nextCursor").ValueKind);
    }

    [Fact]
    public async Task Pagination_returns_20_then_3_without_duplicates_and_default_matches_20()
    {
        var q = fixture.Prefix + "search";
        var first = await fixture.Search(q);
        Assert.Equal(20, UserSearchFixture.Ids(first).Length);
        var cursor = first.GetProperty("nextCursor").GetString();
        Assert.NotNull(cursor);
        var second = await fixture.Search(q, 20, cursor);
        Assert.Equal(3, UserSearchFixture.Ids(second).Length);
        Assert.Equal(23, UserSearchFixture.Ids(first).Concat(UserSearchFixture.Ids(second)).Distinct().Count());
        Assert.Equal(JsonValueKind.Null, second.GetProperty("nextCursor").ValueKind);
        Assert.Equal(23, UserSearchFixture.Ids(await fixture.Search(q, 50)).Length);
    }

    [Theory]
    [InlineData(1, 400)]
    [InlineData(2, 200)]
    [InlineData(64, 200)]
    [InlineData(65, 400)]
    public async Task ASCII_UTF16_boundaries(int length, int status)
    {
        await fixture.Search(new string('a', length), expected: status);
    }

    [Fact]
    public async Task Supplementary_characters_count_two_and_missing_q_bad_limit_and_NUL_are_validation()
    {
        await fixture.Search(string.Concat(Enumerable.Repeat("😀", 32)), expected: 200);
        await fixture.Search(string.Concat(Enumerable.Repeat("😀", 33)), expected: 400);
        await fixture.Search("ab\0", expected: 400);
        await fixture.Search("ab", limit: 0, expected: 400);
        await fixture.Search("ab", limit: 51, expected: 400);
        foreach (var url in new[] { "/api/v1/users/search", "/api/v1/users/search?q=ab&limit=abc" })
        {
            var response = await fixture.Send(url);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Common.ValidationFailed", body.GetProperty("errorCode").GetString());
        }
    }

    [Fact]
    public async Task Cursor_rejects_other_actor_query_limit_tampering_and_expiry()
    {
        var q = fixture.Prefix + "search";
        var first = await fixture.Search(q);
        var cursor = first.GetProperty("nextCursor").GetString()!;
        foreach (var body in new[] {
            await fixture.Search("other-query", cursor: cursor, expected: 400),
            await fixture.Search(q, limit: 10, cursor: cursor, expected: 400),
            await fixture.Search(q, cursor: cursor, actor: fixture.Users["bao"], expected: 400),
            await fixture.Search(q, cursor: cursor + "tampered", expected: 400),
            await fixture.Search(q, cursor: "", expected: 400) })
            Assert.Equal("CURSOR_INVALID", body.GetProperty("errorCode").GetString());
        try
        {
            fixture.Clock.CursorFuture = true;
            // Session lasts 30d; recheck remains valid at +25h. JWT middleware still uses real time.
            var expired = await fixture.Search(q, cursor: cursor, expected: 400);
            Assert.Equal("CURSOR_INVALID", expired.GetProperty("errorCode").GetString());
        }
        finally { fixture.Clock.CursorFuture = false; }
        await fixture.Search(q);
    }

    [Fact]
    public async Task Cursor_survives_new_host_with_same_persistent_key_ring_and_application_name()
    {
        var q = fixture.Prefix + "search";
        var first = await fixture.Search(q);
        var cursor = first.GetProperty("nextCursor").GetString();
        using var other = fixture.MakeFactory();
        using var client = other.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"/api/v1/users/search?q={Uri.EscapeDataString(q)}&cursor={Uri.EscapeDataString(cursor!)}");
        request.Headers.Authorization = new("Bearer", fixture.Actor.AccessToken);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, UserSearchFixture.Ids(await response.Content.ReadFromJsonAsync<JsonElement>()).Length);
    }

    [Fact]
    public async Task Anonymous_and_revoked_sessions_are_unauthorized_and_search_never_writes_messaging()
    {
        var before = await fixture.MessagingCounts();
        using var anon = fixture.Factory.CreateClient();
        var unauthorized = await anon.GetAsync("/api/v1/users/search?q=ab");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        var actor = await fixture.Login(fixture.Actor.Username);
        using var logout = await fixture.Send("/api/v1/auth/logout", actor,
            new { refreshToken = actor.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        await fixture.Search("ab", actor: actor, expected: 401);
        await fixture.Search(fixture.Prefix + "search");
        Assert.Equal(before, await fixture.MessagingCounts());
    }

    [Fact]
    public async Task Contracts_guard_rejects_mismatched_session_and_invalid_UTF16_without_HTTP_binding()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var directory = scope.ServiceProvider.GetRequiredService<IUserSearchDirectory>();
        var actor = fixture.Actor;
        var request = new UserSearchRequest(actor.Id, actor.SessionId, actor.SecurityStamp, "ab");
        var wrongSession = await directory.SearchAsync(request with { SessionId = Guid.NewGuid() }, default);
        Assert.Equal("Common.Unauthorized", wrongSession.Failure?.Code);
        var wrongStamp = await directory.SearchAsync(request with { SecurityStamp = Guid.NewGuid() }, default);
        Assert.Equal("Common.Unauthorized", wrongStamp.Failure?.Code);
        var invalid = await directory.SearchAsync(request with { Q = "a\ud800" }, default);
        Assert.Equal("Common.ValidationFailed", invalid.Failure?.Code);
    }

    [Fact]
    public async Task Profile_update_refreshes_key_and_preserves_original_display_text()
    {
        var actor = fixture.Users["editable"];
        var name = "ĐẶNG " + fixture.Prefix;
        using var request = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/users/me")
        { Content = JsonContent.Create(new { displayName = name, bio = "", locale = "vi-VN", timezone = "Asia/Ho_Chi_Minh" }) };
        request.Headers.Authorization = new("Bearer", actor.AccessToken);
        var response = await fixture.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(name, profile.GetProperty("displayName").GetString());
        Assert.Contains(actor.Id, UserSearchFixture.Ids(await fixture.Search("đặng " + fixture.Prefix)));
    }
}

public sealed class UserSearchFixture : IAsyncLifetime
{
    public sealed record ActorData(Guid Id, string Username, string AccessToken, string RefreshToken,
        Guid SessionId = default, Guid SecurityStamp = default);
    public sealed class SearchClock : TimeProvider
    {
        public bool CursorFuture { get; set; }
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow.AddHours(CursorFuture ? 25 : 0);
    }
    private readonly SCDCWebApplicationFactory _baseFactory = new();
    private readonly string _keyPath = Path.Combine(Path.GetTempPath(), "dm-search-tests", Guid.NewGuid().ToString("N"));
    private string _connection = null!;
    public string Prefix { get; } = $"ds_{Guid.NewGuid():N}"[..12] + "_";
    public SearchClock Clock { get; } = new();
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;
    public Dictionary<string, ActorData> Users { get; } = [];
    public ActorData Actor => Users["owner"];
    public WebApplicationFactory<Program> MakeFactory() => _baseFactory.WithWebHostBuilder(builder =>
    {
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        { ["Modules:Identity:UserSearch:CursorKeyRingPath"] = _keyPath,
          ["Modules:Identity:UserSearch:ApplicationName"] = Prefix }));
        builder.ConfigureServices(services => services.Replace(ServiceDescriptor.Singleton<TimeProvider>(Clock)));
    });
    public async Task InitializeAsync()
    {
        Factory = MakeFactory(); Client = Factory.CreateClient();
        _connection = Factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("Database")!;
        Assert.EndsWith("_test", new NpgsqlConnectionStringBuilder(_connection).Database);
        await Register("owner", "Owner " + Prefix);
        await Register("bao", "Bảo " + Prefix);
        await Register("chi", ("Bảo " + Prefix).Normalize(NormalizationForm.FormD));
        await Register("literal", "P%_\\ " + Prefix);
        await Register("decoy", "PXYY " + Prefix);
        await Register("exact-display", Users["bao"].Username);
        await Register("editable", "Editable " + Prefix);
        await Register("pending", "Bảo " + Prefix, false);
        await Register("disabled", "Bảo " + Prefix);
        await Register("deleted", "Bảo " + Prefix);
        await Register("unverified", "Bảo " + Prefix);
        for (var i = 1; i <= 23; i++) await Register($"search{i:00}", $"Search {i:00} {Prefix}");
        await using var c = new NpgsqlConnection(_connection); await c.OpenAsync();
        await using var command = new NpgsqlCommand("""
            UPDATE identity.users SET status=3 WHERE id=@disabled;
            UPDATE identity.users SET status=4, deleted_at=now() WHERE id=@deleted;
            UPDATE identity.user_emails SET verified_at=NULL WHERE user_id=@unverified;
            """, c);
        command.Parameters.AddWithValue("disabled", Users["disabled"].Id);
        command.Parameters.AddWithValue("deleted", Users["deleted"].Id);
        command.Parameters.AddWithValue("unverified", Users["unverified"].Id);
        await command.ExecuteNonQueryAsync();
    }
    private async Task Register(string alias, string displayName, bool verified = true)
    {
        var username = Prefix + alias.Replace("-", "_");
        var r = await Client.PostAsJsonAsync("/api/v1/auth/register", new
        { username, displayName, email = username + "@example.test", password = "DmDemo2026!Local" });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var b = await r.Content.ReadFromJsonAsync<JsonElement>();
        var id = b.GetProperty("userId").GetGuid();
        if (!verified) { Users[alias] = new(id, username, "", ""); return; }
        var verify = await Client.PostAsJsonAsync("/api/v1/auth/verify-email", new { token = b.GetProperty("developmentVerificationToken").GetString() });
        Assert.Equal(HttpStatusCode.NoContent, verify.StatusCode);
        Users[alias] = await Login(username);
    }
    public async Task<ActorData> Login(string username)
    {
        var r = await Client.PostAsJsonAsync("/api/v1/auth/login", new { login = username, password = "DmDemo2026!Local", deviceName = "DM-P1-test" });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var b = await r.Content.ReadFromJsonAsync<JsonElement>();
        var access = b.GetProperty("accessToken").GetString()!;
        using var jwt = JsonDocument.Parse(WebEncoders.Base64UrlDecode(access.Split('.')[1]));
        return new(b.GetProperty("user").GetProperty("id").GetGuid(), username,
            access, b.GetProperty("refreshToken").GetString()!,
            jwt.RootElement.GetProperty("sid").GetGuid(), jwt.RootElement.GetProperty("sst").GetGuid());
    }
    public async Task<HttpResponseMessage> Send(string path, ActorData? actor = null, object? body = null)
    {
        using var request = new HttpRequestMessage(body is null ? HttpMethod.Get : HttpMethod.Post, path);
        request.Headers.Authorization = new("Bearer", (actor ?? Actor).AccessToken);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await Client.SendAsync(request);
    }
    public async Task<JsonElement> Search(string q, int limit = 20, string? cursor = null, ActorData? actor = null, int expected = 200)
    {
        var url = $"/api/v1/users/search?q={Uri.EscapeDataString(q)}&limit={limit}";
        if (cursor is not null) url += "&cursor=" + Uri.EscapeDataString(cursor);
        using var r = await Send(url, actor);
        Assert.Equal(expected, (int)r.StatusCode);
        return await r.Content.ReadFromJsonAsync<JsonElement>();
    }
    public static Guid[] Ids(JsonElement body) => body.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ToArray();
    public async Task<string> MessagingCounts()
    {
        await using var c = new NpgsqlConnection(_connection); await c.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT concat((SELECT count(*) FROM messaging.direct_conversations),':',(SELECT count(*) FROM messaging.space_members),':',(SELECT count(*) FROM messaging.messages))", c);
        return (string)(await command.ExecuteScalarAsync())!;
    }
    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (_connection is not null)
        {
            await using var c = new NpgsqlConnection(_connection); await c.OpenAsync();
            await using var command = new NpgsqlCommand("""
                DELETE FROM integration.outbox_events WHERE aggregate_id=ANY(@ids);
                DELETE FROM audit.security_events WHERE user_id=ANY(@ids);
                DELETE FROM identity.users WHERE id=ANY(@ids);
                """, c);
            command.Parameters.AddWithValue("ids", Users.Values.Select(x => x.Id).ToArray());
            await command.ExecuteNonQueryAsync();
        }
        if (Factory is not null) await Factory.DisposeAsync();
        await _baseFactory.DisposeAsync();
        // No recursive filesystem deletion: keys remain in isolated test temp directory.
    }
}
