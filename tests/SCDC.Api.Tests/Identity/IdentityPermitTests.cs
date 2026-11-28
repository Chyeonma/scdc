using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SCDC.Api.Tests.Infrastructure;
using SCDC.BuildingBlocks.Infrastructure.Persistence;
using SCDC.Contracts.Identity;

namespace SCDC.Api.Tests.Identity;

public sealed class IdentityPermitTests : IAsyncLifetime
{
    private readonly SCDCWebApplicationFactory _factory = new();
    private HttpClient _client = null!;
    private NpgsqlDataSource _source = null!;
    private AccountActor _actor = null!;
    private string _connectionString = null!;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        await SCDCWebApplicationFactory.WaitForAccessReadyAsync(_client);
        _connectionString = _factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("TestDatabase")!;
        Assert.EndsWith("_test", new NpgsqlConnectionStringBuilder(_connectionString).Database);
        _source = NpgsqlDataSource.Create(_connectionString);
        var name = $"guard_{Guid.NewGuid():N}"[..25];
        var registration = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            username = name, displayName = "Guard test", email = $"{name}@example.test", password = "Initial123"
        });
        registration.EnsureSuccessStatusCode();
        var registered = await registration.Content.ReadFromJsonAsync<JsonElement>();
        var verify = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", new
        { token = registered.GetProperty("developmentVerificationToken").GetString() });
        verify.EnsureSuccessStatusCode();
        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new { login = name, password = "Initial123" });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        var payload = body.GetProperty("accessToken").GetString()!.Split('.')[1].Replace('-', '+').Replace('_', '/');
        using var claims = JsonDocument.Parse(Convert.FromBase64String(payload.PadRight((payload.Length + 3) / 4 * 4, '=')));
        _actor = new(claims.RootElement.GetProperty("sub").GetGuid(), claims.RootElement.GetProperty("sid").GetGuid(),
            claims.RootElement.GetProperty("sst").GetGuid(), DateTimeOffset.FromUnixTimeSeconds(claims.RootElement.GetProperty("exp").GetInt64()));
    }

    [Theory]
    [InlineData("revoked", "Common.Unauthorized")]
    [InlineData("stamp", "Common.Unauthorized")]
    [InlineData("expired", "Common.Unauthorized")]
    [InlineData("unverified", "ACCOUNT_ACCESS_DENIED")]
    [InlineData("inactive", "Common.Unauthorized")]
    public async Task Permit_rechecks_current_account_and_session(string change, string expected)
    {
        var sql = change switch
        {
            "revoked" => "UPDATE identity.auth_sessions SET revoked_at = clock_timestamp() WHERE id = @session",
            "stamp" => "UPDATE identity.user_security_states SET security_stamp = gen_random_uuid() WHERE user_id = @user",
            "expired" => "UPDATE identity.auth_sessions SET created_at = clock_timestamp() - interval '2 days', expires_at = clock_timestamp() - interval '1 day' WHERE id = @session",
            "unverified" => "UPDATE identity.user_emails SET verified_at = NULL WHERE user_id = @user",
            _ => "UPDATE identity.users SET status = 3 WHERE id = @user"
        };
        await using var mutate = _source.CreateCommand(sql);
        mutate.Parameters.AddWithValue("user", _actor.UserId);
        mutate.Parameters.AddWithValue("session", _actor.SessionId);
        await mutate.ExecuteNonQueryAsync();
        using var services = _factory.Services.CreateScope();
        var issuer = services.ServiceProvider.GetRequiredKeyedService<IIdentityPermitIssuer>(AccessOwner.Community);
        var failure = await Assert.ThrowsAsync<AccessCheckException>(() => issuer.AcquireAsync(Request(), default));
        Assert.Equal(expected, failure.Code);
    }

    [Fact]
    public async Task Permit_releases_identity_locks_and_reacquisition_keeps_the_same_epochs()
    {
        using var services = _factory.Services.CreateScope();
        var issuer = services.ServiceProvider.GetRequiredKeyedService<IIdentityPermitIssuer>(AccessOwner.Community);
        var request = Request();
        var first = await issuer.AcquireAsync(request, default);
        Assert.Equal(AccessOwner.Community, first.Owner);
        Assert.True(first.ExpiresAt > DateTimeOffset.UtcNow);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var writer = new NpgsqlCommand("SELECT id FROM identity.users WHERE id = @user FOR NO KEY UPDATE", connection)
        { CommandTimeout = 2 };
        writer.Parameters.AddWithValue("user", _actor.UserId);
        Assert.Equal(_actor.UserId, await writer.ExecuteScalarAsync());
        var next = await issuer.AcquireAsync(request, default);
        Assert.Equal(first.Scopes, next.Scopes);
    }

    private PermitRequest Request() => new(new(_actor.UserId, _actor.SessionId, _actor.SecurityStamp, _actor.AccessExpiresAt),
        IdentityPurpose.CommunityWrite, _actor.UserId, Guid.NewGuid());

    [Fact]
    public async Task Uncommitted_work_scope_rolls_back_its_writes()
    {
        var id = Guid.CreateVersion7();
        await using (var scope = await new RelationalWorkScopeFactory(_source).OpenAsync(default))
        {
            await using var command = scope.CreateCommand("INSERT INTO community.outbox_events (id,event_type,aggregate_type,aggregate_id,payload) VALUES (@id,'Test.Rollback','test',@id,'{}')");
            command.Parameters.AddWithValue("id", id);
            await command.ExecuteNonQueryAsync();
        }
        await using var read = _source.CreateCommand("SELECT count(*) FROM community.outbox_events WHERE id = @id");
        read.Parameters.AddWithValue("id", id);
        Assert.Equal(0L, await read.ExecuteScalarAsync());
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _source.DisposeAsync();
    }
}
