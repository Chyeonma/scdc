using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SCDC.Api.Tests.Infrastructure;
using SCDC.BuildingBlocks.Infrastructure.Persistence;
using SCDC.Contracts.Identity;

namespace SCDC.Api.Tests.Community;

public sealed class AccountAccessGuardTests : IAsyncLifetime
{
    private readonly SCDCWebApplicationFactory _factory = new();
    private HttpClient _client = null!;
    private NpgsqlDataSource _source = null!;
    private AccountActor _actor = null!;
    private string _connectionString = null!;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        _connectionString = _factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("Database")!;
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
            claims.RootElement.GetProperty("sst").GetGuid());
    }

    [Theory]
    [InlineData("revoked", AccountAccessStatus.InvalidSession)]
    [InlineData("stamp", AccountAccessStatus.InvalidSession)]
    [InlineData("expired", AccountAccessStatus.InvalidSession)]
    [InlineData("unverified", AccountAccessStatus.AccountDenied)]
    [InlineData("inactive", AccountAccessStatus.AccountDenied)]
    public async Task Guard_rechecks_current_account_and_session(string change, AccountAccessStatus expected)
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
        await using var scope = await new RelationalWorkScopeFactory(_source).OpenAsync(default);
        using var services = _factory.Services.CreateScope();
        var check = await services.ServiceProvider.GetRequiredService<IAccountAccessGuard>().AcquireAsync(_actor, scope.Transaction, default);
        Assert.Equal(expected, check.Status);
    }

    [Fact]
    public async Task Guard_holds_security_writer_until_transaction_finishes()
    {
        await using var scope = await new RelationalWorkScopeFactory(_source).OpenAsync(default);
        using var services = _factory.Services.CreateScope();
        var check = await services.ServiceProvider.GetRequiredService<IAccountAccessGuard>().AcquireAsync(_actor, scope.Transaction, default);
        Assert.True(check.IsAllowedAt(DateTimeOffset.UtcNow));

        var writerName = $"guard-writer-{Guid.NewGuid():N}";
        await using var connection = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(_connectionString)
        { ApplicationName = writerName }.ConnectionString);
        await connection.OpenAsync();
        await using var writer = new NpgsqlCommand("SELECT id FROM identity.users WHERE id = @user FOR NO KEY UPDATE", connection)
        { CommandTimeout = 10 };
        writer.Parameters.AddWithValue("user", _actor.UserId);
        var pending = writer.ExecuteScalarAsync();
        try
        {
            var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
            var blocked = false;
            while (DateTimeOffset.UtcNow < deadline && !pending.IsCompleted)
            {
                await using var probe = _source.CreateCommand("SELECT EXISTS (SELECT 1 FROM pg_stat_activity WHERE application_name = @name AND wait_event_type = 'Lock')");
                probe.Parameters.AddWithValue("name", writerName);
                if (await probe.ExecuteScalarAsync() is true) { blocked = true; break; }
                await Task.Delay(20);
            }
            Assert.True(blocked, "The competing Identity writer must wait for the guard's transaction.");
        }
        finally
        {
            await scope.CommitAsync(default);
        }
        Assert.Equal(_actor.UserId, await pending);
    }

    [Fact]
    public async Task Uncommitted_work_scope_rolls_back_its_writes()
    {
        var id = Guid.CreateVersion7();
        await using (var scope = await new RelationalWorkScopeFactory(_source).OpenAsync(default))
        {
            await using var command = scope.CreateCommand("INSERT INTO integration.outbox_events (id,event_type,aggregate_type,aggregate_id,payload) VALUES (@id,'Test.Rollback','test',@id,'{}')");
            command.Parameters.AddWithValue("id", id);
            await command.ExecuteNonQueryAsync();
        }
        await using var read = _source.CreateCommand("SELECT count(*) FROM integration.outbox_events WHERE id = @id");
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
