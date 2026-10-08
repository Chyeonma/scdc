using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;
using SCDC.Api.Tests.Infrastructure;
using SCDC.Modules.Community.Infrastructure;

namespace SCDC.Api.Tests.Community;

public sealed partial class CommunityApiTests : IAsyncLifetime
{
    private readonly SCDCWebApplicationFactory _factory = new();
    private HttpClient _client = null!;
    private string _connectionString = null!;
    private Actor _owner = null!;
    private Actor _other = null!;
    private sealed record Actor(Guid UserId, Guid SessionId, string AccessToken, string RefreshToken);
    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        _connectionString = _factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("Database")!;
        Assert.EndsWith("_test", new NpgsqlConnectionStringBuilder(_connectionString).Database);
        _owner = await RegisterAsync();
        _other = await RegisterAsync();
    }
    private async Task<Actor> RegisterAsync()
    {
        var name = $"com_{Guid.NewGuid():N}"[..24];
        var registered = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            username = name,
            displayName = "Community test",
            email = $"{name}@example.test",
            password = "Initial123"
        });
        registered.EnsureSuccessStatusCode();
        var body = await JsonAsync(registered);
        var verify = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", new
        {
            token = body.GetProperty("developmentVerificationToken").GetString()
        });
        verify.EnsureSuccessStatusCode();
        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            login = name,
            password = "Initial123"
        });
        login.EnsureSuccessStatusCode();
        var auth = await JsonAsync(login);
        var token = auth.GetProperty("accessToken").GetString()!;
        var payload = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
        using var claims = JsonDocument.Parse(Convert.FromBase64String(payload.PadRight((payload.Length + 3) / 4 * 4, '=')));
        return new(body.GetProperty("userId").GetGuid(), claims.RootElement.GetProperty("sid").GetGuid(), token, auth.GetProperty("refreshToken").GetString()!);
    }
    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) => JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, Actor actor, object? body = null, HttpClient? client = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", actor.AccessToken);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return (client ?? _client).SendAsync(request);
    }
    private Task<HttpResponseMessage> CreateAsync(Guid? operation = null, string name = "Test community", string? description = null, string? visibility = null, Actor? actor = null, HttpClient? client = null)
    {
        var body = new Dictionary<string, object?> { ["clientOperationId"] = operation ?? Guid.NewGuid(), ["name"] = name, ["description"] = description };
        if (visibility is not null)
            body["visibility"] = visibility;
        return SendAsync(HttpMethod.Post, "/api/v1/servers", actor ?? _owner, body, client);
    }
    private async Task<object?> SqlAsync(string sql, params (string, object)[] parameters)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = 10 };
        foreach (var (key, value) in parameters)
            command.Parameters.AddWithValue(key, value);
        return await command.ExecuteScalarAsync();
    }
    private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var error = await JsonAsync(response);
        Assert.Equal(code, error.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Create_initializes_owner_everyone_and_pending_outbox_and_retry_reads_current_detail()
    {
        var operation = Guid.NewGuid();
        var first = await CreateAsync(operation, "\u00a0Nhóm Việt 👩‍💻\u00a0", "Hai chủ đề\r\nMột cộng đồng");
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var server = await JsonAsync(first);
        var id = server.GetProperty("id").GetGuid();
        Assert.Equal(7, id.Version);
        Assert.Equal("Nhóm Việt 👩‍💻", server.GetProperty("name").GetString());
        Assert.Equal("Hai chủ đề\nMột cộng đồng", server.GetProperty("description").GetString());
        Assert.Equal("public", server.GetProperty("visibility").GetString());
        Assert.Equal("immediate", server.GetProperty("joinMode").GetString());
        Assert.Equal("1", server.GetProperty("version").GetString());
        Assert.Equal("1", server.GetProperty("accessVersion").GetString());
        Assert.Equal(_owner.UserId, server.GetProperty("ownerUserId").GetGuid());
        Assert.Equal(5, server.GetProperty("effectivePermissions").GetArrayLength());
        var member = server.GetProperty("myMembership");
        Assert.Equal(7, member.GetProperty("membershipId").GetGuid().Version);
        Assert.Equal("active", member.GetProperty("status").GetString());
        Assert.Equal($"/api/v1/servers/{id}", first.Headers.Location?.ToString());
        Assert.Equal(1L, await SqlAsync("SELECT count(*) FROM community.roles WHERE server_id=@id AND name='@everyone' AND is_default AND is_system", ("id", id)));
        Assert.Equal(0L, await SqlAsync("SELECT count(*) FROM community.member_roles WHERE server_id=@id", ("id", id)));
        Assert.Equal(1L, await SqlAsync("SELECT count(*) FROM integration.outbox_events WHERE aggregate_id=@id AND event_type='Community.ServerCreated.v1' AND published_at IS NULL", ("id", id)));
        await SqlAsync("UPDATE community.servers SET name='Updated title',search_name='updated title' WHERE id=@id", ("id", id));
        var replay = await CreateAsync(operation, "Nhóm Việt 👩‍💻", "Hai chủ đề\nMột cộng đồng", "public");
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        var current = await JsonAsync(replay);
        Assert.Equal(id, current.GetProperty("id").GetGuid());
        Assert.Equal("Updated title", current.GetProperty("name").GetString());
        Assert.Equal("2", current.GetProperty("version").GetString());
        Assert.Contains(id, SearchIds(await JsonAsync(await SearchAsync("Updated title"))));
        Assert.DoesNotContain(id, SearchIds(await JsonAsync(await SearchAsync("Nhóm Việt 👩‍💻"))));
        Assert.Equal(first.Headers.Location, replay.Headers.Location);
        await AssertErrorAsync(await CreateAsync(operation, "Changed payload"), HttpStatusCode.Conflict, "OPERATION_CONFLICT");
        Assert.Equal(1L, await SqlAsync("SELECT count(*) FROM community.operations WHERE actor_user_id=@actor AND client_operation_id=@op", ("actor", _owner.UserId), ("op", operation)));
    }

    [Theory]
    [InlineData("\u200b", null, "name")]
    [InlineData("a", null, "name")]
    [InlineData("first\nsecond", null, "name")]
    [InlineData("hello\u2028world", null, "name")]
    [InlineData("valid", "\0", "description")]
    public async Task Invalid_text_returns_field_errors_and_writes_nothing(string name, string? description, string field)
    {
        var response = await CreateAsync(name: name, description: description);
        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "VALIDATION_FAILED");
        Assert.True((await JsonAsync(response)).GetProperty("errors").TryGetProperty(field, out _));
        Assert.Equal(0L, await SqlAsync("SELECT count(*) FROM community.servers WHERE owner_user_id=@actor", ("actor", _owner.UserId)));
    }
    [Fact]
    public async Task Utf16_limits_emoji_and_preserved_whitespace_follow_the_contract()
    {
        var accepted = await CreateAsync(name: "😀", description: new string('x', 998) + "😀");
        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await CreateAsync(name: new string('x', 99) + "😀")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await CreateAsync(description: new string('x', 999) + "😀")).StatusCode);
        var blankDescription = await CreateAsync(name: "A\u200dB", description: " \r\n ");
        Assert.Equal(" \n ", (await JsonAsync(blankDescription)).GetProperty("description").GetString());
        var empty = await CreateAsync(description: "");
        Assert.Equal(JsonValueKind.Null, (await JsonAsync(empty)).GetProperty("description").ValueKind);
    }
    [Fact]
    public async Task Unknown_fields_missing_required_data_and_non_rfc_operation_ids_are_rejected()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(HttpMethod.Post, "/api/v1/servers", _owner, new
        {
            clientOperationId = Guid.NewGuid(),
            name = "Valid",
            ownerUserId = _other.UserId
        })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(HttpMethod.Post, "/api/v1/servers", _owner, new
        {
            name = "Valid"
        })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await CreateAsync(Guid.CreateVersion7())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await CreateAsync(Guid.Parse("00000000-0000-4000-0000-000000000001"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await CreateAsync(visibility: "PUBLIC")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PostAsJsonAsync("/api/v1/servers", new
        {
            clientOperationId = Guid.NewGuid(),
            name = "Valid"
        })).StatusCode);
    }
    [Fact]
    public async Task Public_projection_hides_private_data_and_private_own_left_membership_does_not_restore_access()
    {
        var publicServer = await JsonAsync(await CreateAsync());
        var publicId = publicServer.GetProperty("id").GetGuid();
        var summary = await JsonAsync(await SendAsync(HttpMethod.Get, $"/api/v1/servers/{publicId}", _other));
        Assert.Equal(new[] { "description", "id", "joinMode", "name", "version", "visibility" }, summary.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Get, $"/api/v1/servers/{publicId}/membership/me", _other)).StatusCode);
        var privateServer = await JsonAsync(await CreateAsync(visibility: "private"));
        var privateId = privateServer.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Get, $"/api/v1/servers/{privateId}", _other)).StatusCode);
        await SqlAsync("INSERT INTO community.server_members(server_id,user_id,status,left_at) VALUES(@id,@actor,2,clock_timestamp())", ("id", privateId), ("actor", _other.UserId));
        var own = await JsonAsync(await SendAsync(HttpMethod.Get, $"/api/v1/servers/{privateId}/membership/me", _other));
        Assert.Equal("left", own.GetProperty("status").GetString());
        Assert.Equal(_other.UserId, own.GetProperty("userId").GetGuid());
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Get, $"/api/v1/servers/{privateId}", _other)).StatusCode);
        var list = await JsonAsync(await SendAsync(HttpMethod.Get, "/api/v1/servers", _other));
        Assert.Empty(list.GetProperty("items").EnumerateArray());
        Assert.Equal((short)2, await SqlAsync("SELECT status FROM community.server_members WHERE server_id=@id AND user_id=@actor", ("id", privateId), ("actor", _other.UserId)));
        await SqlAsync("UPDATE community.servers SET status=2 WHERE id=@id", ("id", publicId));
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Get, $"/api/v1/servers/{publicId}", _other)).StatusCode);
    }
    [Fact]
    public async Task Member_permissions_use_only_supported_assigned_custom_roles()
    {
        var id = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        var role = Guid.CreateVersion7();
        await SqlAsync("""
            BEGIN;
            INSERT INTO community.server_members(server_id,user_id) VALUES(@id,@actor);
            INSERT INTO community.roles(id,server_id,name,name_key) VALUES(@role,@id,'Moderators','moderators');
            INSERT INTO community.permissions(code,description) VALUES('manage_invites','test') ON CONFLICT DO NOTHING;
            INSERT INTO community.role_permissions(role_id,permission_code) VALUES(@role,'manage_invites');
            INSERT INTO community.member_roles(server_id,user_id,role_id,membership_id) SELECT @id,@actor,@role,membership_id FROM community.server_members WHERE server_id=@id AND user_id=@actor;
            COMMIT;
            """, ("id", id), ("actor", _other.UserId), ("role", role));
        var detail = await JsonAsync(await SendAsync(HttpMethod.Get, $"/api/v1/servers/{id}", _other));
        Assert.Equal(new[] { "manage_invites" }, detail.GetProperty("effectivePermissions").EnumerateArray().Select(p => p.GetString()).ToArray());
    }
    [Fact]
    public async Task List_uses_keyset_and_cursor_rejects_cross_actor_limit_and_tamper_and_survives_restart()
    {
        for (var i = 0; i < 3; i++)
            (await CreateAsync(name: $"Server {i}")).EnsureSuccessStatusCode();
        var first = await JsonAsync(await SendAsync(HttpMethod.Get, "/api/v1/servers?limit=2", _owner));
        var cursor = first.GetProperty("nextCursor").GetString()!;
        Assert.Equal(2, first.GetProperty("items").GetArrayLength());
        var query = $"/api/v1/servers?limit=2&cursor={Uri.EscapeDataString(cursor)}";
        await using var restarted = new SCDCWebApplicationFactory();
        using var client = restarted.CreateClient();
        var second = await JsonAsync(await SendAsync(HttpMethod.Get, query, _owner, client: client));
        Assert.Single(second.GetProperty("items").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, second.GetProperty("nextCursor").ValueKind);
        var ids = first.GetProperty("items").EnumerateArray().Concat(second.GetProperty("items").EnumerateArray()).Select(s => s.GetProperty("id").GetGuid()).ToArray();
        Assert.Equal(3, ids.Distinct().Count());
        await AssertErrorAsync(await SendAsync(HttpMethod.Get, query, _other), HttpStatusCode.BadRequest, "CURSOR_INVALID");
        await AssertErrorAsync(await SendAsync(HttpMethod.Get, query.Replace("limit=2", "limit=1"), _owner), HttpStatusCode.BadRequest, "CURSOR_INVALID");
        await AssertErrorAsync(await SendAsync(HttpMethod.Get, $"/api/v1/servers?limit=2&cursor=x{cursor}", _owner), HttpStatusCode.BadRequest, "CURSOR_INVALID");
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(HttpMethod.Get, "/api/v1/servers?limit=51", _owner)).StatusCode);
    }
    [Fact]
    public async Task Concurrent_retries_and_same_key_with_another_actor_create_one_resource_per_actor()
    {
        var operation = Guid.NewGuid();
        var responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => CreateAsync(operation)));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(3, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
        var ids = new List<Guid>();
        foreach (var response in responses)
            ids.Add((await JsonAsync(response)).GetProperty("id").GetGuid());
        Assert.Single(ids.Distinct());
        Assert.Equal(1L, await SqlAsync("SELECT count(*) FROM community.servers WHERE owner_user_id=@actor", ("actor", _owner.UserId)));
        Assert.Equal(1L, await SqlAsync("SELECT count(*) FROM integration.outbox_events WHERE aggregate_id=@id", ("id", ids[0])));
        Assert.Equal(HttpStatusCode.Created, (await CreateAsync(operation, actor: _other)).StatusCode);
    }
    [Fact]
    public async Task Persisted_retry_survives_restart_rotation_and_missing_old_key_fails_closed()
    {
        var operation = Guid.NewGuid();
        var first = await CreateAsync(operation);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var id = (await JsonAsync(first)).GetProperty("id").GetGuid();
        await using var restarted = new SCDCWebApplicationFactory();
        using var client = restarted.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await CreateAsync(operation, client: client)).StatusCode);
        var original = _factory.Services.GetRequiredService<IOptionsMonitor<CommunityOptions>>().CurrentValue;
        var rotated = new CommunityOptions
        {
            KeyRingPath = original.KeyRingPath,
            Operations = new OperationKeyOptions
            {
                ActiveKeyId = "next",
                Keys = new(original.Operations.Keys) { ["next"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) }
            }
        };
        using var rotatedFactory = _factory.WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<IOptionsMonitor<CommunityOptions>>(new FixedMonitor(rotated))));
        using var rotatedClient = rotatedFactory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await CreateAsync(operation, client: rotatedClient)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await CreateAsync(client: rotatedClient)).StatusCode);
        rotated.Operations.Keys.Remove("test");
        await AssertErrorAsync(await CreateAsync(operation, client: rotatedClient), HttpStatusCode.ServiceUnavailable, "FINGERPRINT_KEY_UNAVAILABLE");
        Assert.Equal(1L, await SqlAsync("SELECT count(*) FROM integration.outbox_events WHERE aggregate_id=@id", ("id", id)));
    }
    [Theory]
    [InlineData("unverified", HttpStatusCode.Forbidden)]
    [InlineData("inactive", HttpStatusCode.Unauthorized)]
    [InlineData("revoked", HttpStatusCode.Unauthorized)]
    [InlineData("stamp", HttpStatusCode.Unauthorized)]
    public async Task Account_security_changes_block_creation_without_writes(string change, HttpStatusCode status)
    {
        await SqlAsync(change switch
        {
            "unverified" => "UPDATE identity.user_emails SET verified_at=NULL WHERE user_id=@actor",
            "inactive" => "UPDATE identity.users SET status=3 WHERE id=@actor",
            "revoked" => "UPDATE identity.auth_sessions SET revoked_at=clock_timestamp() WHERE id=@session",
            _ => "UPDATE identity.user_security_states SET security_stamp=gen_random_uuid() WHERE user_id=@actor"
        }, ("actor", _owner.UserId), ("session", _owner.SessionId));
        Assert.Equal(status, (await CreateAsync()).StatusCode);
        Assert.Equal(0L, await SqlAsync("SELECT count(*) FROM community.operations WHERE actor_user_id=@actor", ("actor", _owner.UserId)));
    }
    [Theory]
    [InlineData("public", HttpStatusCode.Forbidden, "PERMISSION_DENIED")]
    [InlineData("private", HttpStatusCode.NotFound, "RESOURCE_NOT_FOUND")]
    public async Task Replay_rechecks_current_membership_after_ownership_changes(string visibility, HttpStatusCode status, string error)
    {
        var operation = Guid.NewGuid();
        var first = await CreateAsync(operation, visibility: visibility);
        var id = (await JsonAsync(first)).GetProperty("id").GetGuid();
        await SqlAsync("""
            BEGIN;
            INSERT INTO community.server_members(server_id,user_id) VALUES(@id,@nextOwner);
            UPDATE community.servers SET owner_user_id=@nextOwner,access_version=access_version+1 WHERE id=@id;
            UPDATE community.server_members SET status=2,left_at=clock_timestamp(),version=version+1 WHERE server_id=@id AND user_id=@actor;
            COMMIT;
            """, ("id", id), ("nextOwner", _other.UserId), ("actor", _owner.UserId));
        await AssertErrorAsync(await CreateAsync(operation, visibility: visibility), status, error);
        Assert.Equal(1L, await SqlAsync("SELECT count(*) FROM integration.outbox_events WHERE aggregate_id=@id", ("id", id)));
    }
    [Fact]
    public async Task Missing_operation_resource_is_an_internal_consistency_error()
    {
        var operation = Guid.NewGuid();
        (await CreateAsync(operation)).EnsureSuccessStatusCode();
        await SqlAsync("UPDATE community.operations SET resource_id=@missing WHERE actor_user_id=@actor AND client_operation_id=@op", ("missing", Guid.CreateVersion7()), ("actor", _owner.UserId), ("op", operation));
        var replay = await CreateAsync(operation);
        Assert.Equal(HttpStatusCode.InternalServerError, replay.StatusCode);
        Assert.DoesNotContain("Operation references", await replay.Content.ReadAsStringAsync());
        Assert.Equal(1L, await SqlAsync("SELECT count(*) FROM community.servers WHERE owner_user_id=@actor", ("actor", _owner.UserId)));
    }
    [Fact]
    public async Task Null_visibility_and_unpaired_json_surrogate_return_bad_request()
    {
        await AssertErrorAsync(await SendAsync(HttpMethod.Post, "/api/v1/servers", _owner, new
        {
            clientOperationId = Guid.NewGuid(),
            name = "Valid",
            visibility = (string?)null
        }), HttpStatusCode.BadRequest, "VALIDATION_FAILED");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/servers");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _owner.AccessToken);
        request.Content = new StringContent("{\"clientOperationId\":\"" + Guid.NewGuid() + "\",\"name\":\"\\uD800name\"}", System.Text.Encoding.UTF8, "application/json");
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(request)).StatusCode);
    }
    [Fact]
    public async Task Key_ring_storage_failure_returns_service_unavailable_for_encode_and_decode()
    {
        (await CreateAsync()).EnsureSuccessStatusCode();
        (await CreateAsync()).EnsureSuccessStatusCode();
        var page = await JsonAsync(await SendAsync(HttpMethod.Get, "/api/v1/servers?limit=1", _owner));
        var cursor = page.GetProperty("nextCursor").GetString()!;
        var path = Path.Combine(Path.GetTempPath(), $"scdc-unavailable-keyring-{Guid.NewGuid():N}");
        await File.WriteAllTextAsync(path, "not a directory");
        try
        {
            using var brokenFactory = _factory.WithWebHostBuilder(b => b.ConfigureServices(s => s.PostConfigure<CommunityOptions>(value => value.KeyRingPath = path)));
            using var client = brokenFactory.CreateClient();
            await AssertErrorAsync(await SendAsync(HttpMethod.Get, "/api/v1/servers?limit=1", _owner, client: client), HttpStatusCode.ServiceUnavailable, "CURSOR_KEY_UNAVAILABLE");
            await AssertErrorAsync(await SendAsync(HttpMethod.Get, $"/api/v1/servers?limit=1&cursor={Uri.EscapeDataString(cursor)}", _owner, client: client), HttpStatusCode.ServiceUnavailable, "CURSOR_KEY_UNAVAILABLE");
        }
        finally { File.Delete(path); }
    }
    private sealed class FixedMonitor(CommunityOptions value) : IOptionsMonitor<CommunityOptions>
    {
        public CommunityOptions CurrentValue => value;
        public CommunityOptions Get(string? name) => value;
        public IDisposable? OnChange(Action<CommunityOptions, string?> listener) => null;
    }
    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }
}
