using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SCDC.Api.Tests.Infrastructure;
using SCDC.Contracts.Identity;
using SCDC.Modules.Community.Features.Servers.Application;

namespace SCDC.Api.Tests.Community;

public sealed partial class CommunityApiTests
{
    private Task<HttpResponseMessage> SearchAsync(string? query, int limit = 20, string? cursor = null, Actor? actor = null, HttpClient? client = null)
    {
        var url = $"/api/v1/servers/search?limit={limit}" + (query is null ? "" : $"&q={Uri.EscapeDataString(query)}")
            + (cursor is null ? "" : $"&cursor={Uri.EscapeDataString(cursor)}");
        return SendAsync(HttpMethod.Get, url, actor ?? _other, client: client);
    }
    private static Guid[] SearchIds(JsonElement page) => page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).ToArray();

    [Fact]
    public async Task Search_only_returns_public_active_summaries_and_never_creates_membership()
    {
        var query = "privacy" + Guid.NewGuid().ToString("N")[..8];
        var publicId = (await JsonAsync(await CreateAsync(name: query + " public"))).GetProperty("id").GetGuid();
        (await CreateAsync(name: query + " private", visibility: "private")).EnsureSuccessStatusCode();
        var inactive = (await JsonAsync(await CreateAsync(name: query + " inactive"))).GetProperty("id").GetGuid();
        var deleted = (await JsonAsync(await CreateAsync(name: query + " deleted"))).GetProperty("id").GetGuid();
        await SqlAsync("UPDATE community.servers SET status=2 WHERE id=@id", ("id", inactive));
        await SqlAsync("UPDATE community.servers SET status=3,deleted_at=clock_timestamp() WHERE id=@id", ("id", deleted));
        foreach (var actor in new[] { _owner, _other })
        {
            var response = await SearchAsync(query, actor: actor);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var page = await JsonAsync(response);
            Assert.Equal(new[] { publicId }, SearchIds(page));
            var item = page.GetProperty("items")[0];
            Assert.Equal(new[] { "description", "id", "joinMode", "name", "version", "visibility" }, item.EnumerateObject().Select(p => p.Name).Order().ToArray());
        }
        Assert.Equal(0L, await OtherMembershipsAsync(publicId));
        Assert.Equal(0L, await JoinedEventsAsync(publicId));
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Get, $"/api/v1/servers/{publicId}/membership/me", _other)).StatusCode);
    }

    [Fact]
    public async Task Search_normalizes_case_and_nfc_preserves_accents_and_ranks_duplicate_exact_names_first()
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        var exact1 = (await JsonAsync(await CreateAsync(name: tag + " VIỆT"))).GetProperty("id").GetGuid();
        var exact2 = (await JsonAsync(await CreateAsync(name: tag + " Việt"))).GetProperty("id").GetGuid();
        var partial = (await JsonAsync(await CreateAsync(name: "A " + tag + " Việt group"))).GetProperty("id").GetGuid();
        var unaccented = (await JsonAsync(await CreateAsync(name: tag + " Viet"))).GetProperty("id").GetGuid();
        var result = await JsonAsync(await SearchAsync("\u0085" + tag.ToUpperInvariant() + " VIỆT\u3000"));
        var ids = SearchIds(result);
        Assert.Equal(new[] { exact1, exact2 }.OrderBy(id => id.ToString("N"), StringComparer.Ordinal), ids.Take(2));
        Assert.Equal(partial, ids[2]);
        Assert.DoesNotContain(unaccented, ids);
        Assert.Equal(new[] { unaccented }, SearchIds(await JsonAsync(await SearchAsync(tag + " Viet"))));
        Assert.Equal("Việt", (await JsonAsync(await SendAsync(HttpMethod.Get, $"/api/v1/servers/{exact2}", _owner))).GetProperty("name").GetString()![9..]);
    }

    [Theory]
    [InlineData("%_")]
    [InlineData("\\_")]
    [InlineData("\\%")]
    [InlineData("' OR 1=1 --")]
    public async Task Search_treats_sql_and_like_metacharacters_as_literal_text(string literal)
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        var wanted = (await JsonAsync(await CreateAsync(name: tag + " " + literal))).GetProperty("id").GetGuid();
        (await CreateAsync(name: tag + " ordinary")).EnsureSuccessStatusCode();
        Assert.Equal(new[] { wanted }, SearchIds(await JsonAsync(await SearchAsync(tag + " " + literal))));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("  ")]
    [InlineData("\u200b\u034f")]
    [InlineData("OK\nX")]
    [InlineData("OK\0")]
    public async Task Search_rejects_invalid_query_without_falling_back_to_all_servers(string? query)
    {
        var response = await SearchAsync(query);
        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "VALIDATION_FAILED");
        Assert.True((await JsonAsync(response)).GetProperty("errors").TryGetProperty("q", out _));
    }

    [Fact]
    public async Task Search_counts_utf16_at_limits_validates_limit_and_rejects_invalid_internal_utf16()
    {
        var maximum = new string('x', 100);
        var id = (await JsonAsync(await CreateAsync(name: maximum))).GetProperty("id").GetGuid();
        Assert.Contains(id, SearchIds(await JsonAsync(await SearchAsync(maximum))));
        Assert.Equal(HttpStatusCode.OK, (await SearchAsync("😀")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SearchAsync("é")).StatusCode);
        await AssertErrorAsync(await SearchAsync(maximum + "x"), HttpStatusCode.BadRequest, "VALIDATION_FAILED");
        await AssertErrorAsync(await SearchAsync("😀" + new string('x', 99)), HttpStatusCode.BadRequest, "VALIDATION_FAILED");
        foreach (var limit in new[] { 0, 51 }) await AssertErrorAsync(await SearchAsync("OK", limit), HttpStatusCode.BadRequest, "VALIDATION_FAILED");
        using var services = _factory.Services.CreateScope();
        var result = await services.ServiceProvider.GetRequiredService<IServerService>().SearchAsync(new AccountActor(_other.UserId, _other.SessionId, Guid.NewGuid()), "\ud800X", 20, null, default);
        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_FAILED", result.Error.Code);
    }

    [Fact]
    public async Task Search_uses_keyset_across_exact_and_partial_names_and_cursor_binds_actor_query_limit_and_purpose()
    {
        var query = "page" + Guid.NewGuid().ToString("N")[..8];
        var exact = (await JsonAsync(await CreateAsync(name: query))).GetProperty("id").GetGuid();
        for (var i = 0; i < 3; i++) (await CreateAsync(name: query + " duplicate")).EnsureSuccessStatusCode();
        var first = await JsonAsync(await SearchAsync(query, 2));
        Assert.Equal(exact, SearchIds(first)[0]);
        var cursor = first.GetProperty("nextCursor").GetString()!;
        await using var restarted = new SCDCWebApplicationFactory();
        using var client = restarted.CreateClient();
        var second = await JsonAsync(await SearchAsync(query.ToUpperInvariant(), 2, cursor, client: client));
        Assert.Equal(2, SearchIds(second).Length);
        Assert.Equal(4, SearchIds(first).Concat(SearchIds(second)).Distinct().Count());
        Assert.Equal(JsonValueKind.Null, second.GetProperty("nextCursor").ValueKind);
        await AssertErrorAsync(await SearchAsync(query, 2, cursor, actor: _owner), HttpStatusCode.BadRequest, "CURSOR_INVALID");
        await AssertErrorAsync(await SearchAsync(query + " other", 2, cursor), HttpStatusCode.BadRequest, "CURSOR_INVALID");
        await AssertErrorAsync(await SearchAsync(query, 1, cursor), HttpStatusCode.BadRequest, "CURSOR_INVALID");
        await AssertErrorAsync(await SearchAsync(query, 2, "x" + cursor), HttpStatusCode.BadRequest, "CURSOR_INVALID");
        var ownPage = await JsonAsync(await SendAsync(HttpMethod.Get, "/api/v1/servers?limit=2", _owner));
        await AssertErrorAsync(await SearchAsync(query, 2, ownPage.GetProperty("nextCursor").GetString(), actor: _owner), HttpStatusCode.BadRequest, "CURSOR_INVALID");
        await AssertErrorAsync(await SendAsync(HttpMethod.Get, "/api/v1/servers?limit=2&cursor=" + Uri.EscapeDataString(cursor), _other), HttpStatusCode.BadRequest, "CURSOR_INVALID");
    }

    [Fact]
    public async Task Search_old_cursor_excludes_newly_private_inactive_and_deleted_servers()
    {
        var query = "current" + Guid.NewGuid().ToString("N")[..8];
        var ids = new List<Guid>();
        foreach (var suffix in new[] { "A", "B", "C", "D", "E" }) ids.Add((await JsonAsync(await CreateAsync(name: query + suffix))).GetProperty("id").GetGuid());
        var page = await JsonAsync(await SearchAsync(query, 1));
        Assert.Equal(new[] { ids[0] }, SearchIds(page));
        await SqlAsync("UPDATE community.servers SET visibility=2 WHERE id=@id", ("id", ids[1]));
        await SqlAsync("UPDATE community.servers SET status=2 WHERE id=@id", ("id", ids[2]));
        await SqlAsync("UPDATE community.servers SET status=3,deleted_at=clock_timestamp() WHERE id=@id", ("id", ids[3]));
        var next = await JsonAsync(await SearchAsync(query, 1, page.GetProperty("nextCursor").GetString()));
        Assert.Equal(new[] { ids[4] }, SearchIds(next));
        Assert.Equal(JsonValueKind.Null, next.GetProperty("nextCursor").ValueKind);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Get, $"/api/v1/servers/{ids[1]}", _other)).StatusCode);
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("unverified", HttpStatusCode.Forbidden)]
    [InlineData("revoked", HttpStatusCode.Unauthorized)]
    public async Task Search_checks_identity_even_without_membership(string state, HttpStatusCode expected)
    {
        if (state == "unverified") await SqlAsync("UPDATE identity.user_emails SET verified_at=NULL WHERE user_id=@actor", ("actor", _other.UserId));
        if (state == "revoked") await SqlAsync("UPDATE identity.auth_sessions SET revoked_at=clock_timestamp() WHERE id=@id", ("id", _other.SessionId));
        var response = state == "anonymous" ? await _client.GetAsync("/api/v1/servers/search?q=OK") : await SearchAsync("OK");
        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Search_lock_failure_is_temporary_error_instead_of_empty_results()
    {
        var query = "lock" + Guid.NewGuid().ToString("N")[..8];
        (await CreateAsync(name: query)).EnsureSuccessStatusCode();
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var tableLock = new NpgsqlCommand("LOCK TABLE community.servers IN ACCESS EXCLUSIVE MODE", connection, transaction)) await tableLock.ExecuteNonQueryAsync();
        await AssertErrorAsync(await SearchAsync(query), HttpStatusCode.ServiceUnavailable, "COMMUNITY_TEMPORARILY_UNAVAILABLE");
        await transaction.RollbackAsync();
        Assert.Single(SearchIds(await JsonAsync(await SearchAsync(query))));
    }
}
