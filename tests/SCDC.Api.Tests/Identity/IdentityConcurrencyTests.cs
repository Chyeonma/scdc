using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using SCDC.Api.Tests.Infrastructure;

namespace SCDC.Api.Tests.Identity;

public sealed class IdentityConcurrencyTests : IAsyncLifetime
{
    private const string InitialPassword = "Initial123";
    private const string ChangedPassword = "Changed456";
    private readonly string _username = $"race_{Guid.NewGuid():N}"[..25];
    private readonly string _applicationName = $"identity-tests-{Guid.NewGuid():N}";
    private readonly PasswordCheckGate _gate = new();
    private readonly SCDCWebApplicationFactory _baseFactory = new();
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _connectionString = null!;
    private Guid? _userId;
    private string Email => $"{_username}@example.test";

    public async Task InitializeAsync()
    {
        _factory = _baseFactory.WithWebHostBuilder(builder =>
        {
            // Give this host's connections a unique name so concurrency tests can wait
            // for requests to reach PostgreSQL without relying on timing guesses.
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                var connection = new NpgsqlConnectionStringBuilder(configuration.Build().GetConnectionString("Database"))
                {
                    ApplicationName = _applicationName
                };
                _connectionString = connection.ConnectionString;
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Database"] = _connectionString
                });
            });
            builder.ConfigureServices(services =>
            {
                var contextType = services.Single(item => item.ServiceType.FullName
                    == "SCDC.Modules.Identity.Infrastructure.Persistence.IdentityDbContext").ServiceType;
                var optionsType = typeof(DbContextOptions<>).MakeGenericType(contextType);
                var optionsBuilder = (DbContextOptionsBuilder)Activator.CreateInstance(
                    typeof(DbContextOptionsBuilder<>).MakeGenericType(contextType))!;
                optionsBuilder.UseNpgsql(_connectionString);
                services.Replace(ServiceDescriptor.Singleton(optionsType, optionsBuilder.Options));
                services.AddSingleton(_gate);
                var hasher = services.Single(item => item.ServiceType.IsGenericType
                    && item.ServiceType.GetGenericTypeDefinition() == typeof(IPasswordHasher<>));
                services.Replace(ServiceDescriptor.Scoped(hasher.ServiceType,
                    typeof(PausingPasswordHasher<>).MakeGenericType(hasher.ServiceType.GenericTypeArguments)));
            });
        });
        _client = _factory.CreateClient();
        var registration = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            username = _username,
            displayName = "Concurrency test",
            email = Email,
            password = InitialPassword
        });
        registration.EnsureSuccessStatusCode();
        var body = await registration.Content.ReadFromJsonAsync<JsonElement>();
        _userId = body.GetProperty("userId").GetGuid();
        var verification = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", new
        {
            token = body.GetProperty("developmentVerificationToken").GetString()
        });
        Assert.Equal(HttpStatusCode.NoContent, verification.StatusCode);
    }

    [Fact]
    public async Task Changing_password_invalidates_previously_issued_reset_tokens()
    {
        var session = await LoginAsync();
        var resetToken = await RequestResetAsync();
        var change = await ChangePasswordAsync(session);
        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);

        var reset = await ResetPasswordAsync(resetToken);
        Assert.Equal(HttpStatusCode.BadRequest, reset.StatusCode);
        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            login = _username,
            password = ChangedPassword
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_concurrent_login_cannot_survive_a_password_change(bool resetPassword)
    {
        var session = await LoginAsync();
        var resetToken = resetPassword ? await RequestResetAsync() : null;
        _gate.Arm();
        var pendingLogin = _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            login = _username,
            password = InitialPassword
        });
        Task<HttpResponseMessage>? change = null;
        try
        {
            await _gate.Reached.Task.WaitAsync(TimeSpan.FromSeconds(10));
            change = resetPassword ? ResetPasswordAsync(resetToken!) : ChangePasswordAsync(session);
            await WaitForBlockedRequestsAsync(1, change);
        }
        finally
        {
            _gate.Release();
        }

        var lateLogin = await pendingLogin;
        Assert.Equal(HttpStatusCode.OK, lateLogin.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await change!).StatusCode);
        var lateSession = await lateLogin.Content.ReadFromJsonAsync<JsonElement>();
        var me = await AuthorizedAsync(HttpMethod.Get, "/api/v1/users/me", lateSession);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        var refresh = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = lateSession.GetProperty("refreshToken").GetString()
        });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Parallel_wrong_passwords_are_counted_and_trigger_lockout()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var block = new NpgsqlCommand(
            "SELECT user_id FROM identity.user_security_states WHERE user_id = @user_id FOR UPDATE",
            connection, transaction);
        block.Parameters.AddWithValue("user_id", _userId!.Value);
        await block.ExecuteScalarAsync();

        var requests = Enumerable.Range(0, 8).Select(_ => _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            login = _username,
            password = "WrongPassword123"
        })).ToArray();
        try
        {
            await WaitForBlockedRequestsAsync(requests.Length);
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        var responses = await Task.WhenAll(requests);
        Assert.Equal(4, responses.Count(item => item.StatusCode == HttpStatusCode.Unauthorized));
        Assert.Equal(4, responses.Count(item => item.StatusCode == HttpStatusCode.TooManyRequests));
        var correctPassword = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            login = _username,
            password = InitialPassword
        });
        Assert.Equal(HttpStatusCode.TooManyRequests, correctPassword.StatusCode);
    }

    [Fact]
    public async Task Concurrent_reset_requests_can_consume_a_token_only_once()
    {
        var token = await RequestResetAsync();
        var responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => ResetPasswordAsync(token)));
        Assert.Single(responses, item => item.StatusCode == HttpStatusCode.NoContent);
        Assert.Equal(3, responses.Count(item => item.StatusCode == HttpStatusCode.BadRequest));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_login_waiting_for_a_password_change_rechecks_the_password(bool resetPassword)
    {
        var session = await LoginAsync();
        var resetToken = resetPassword ? await RequestResetAsync() : null;
        _gate.Arm();
        var change = resetPassword ? ResetPasswordAsync(resetToken!) : ChangePasswordAsync(session);
        Task<HttpResponseMessage>? login = null;
        try
        {
            await _gate.Reached.Task.WaitAsync(TimeSpan.FromSeconds(10));
            login = _client.PostAsJsonAsync("/api/v1/auth/login", new
            {
                login = _username,
                password = InitialPassword
            });
            await WaitForBlockedRequestsAsync(1, login);
        }
        finally
        {
            _gate.Release();
        }

        Assert.Equal(HttpStatusCode.NoContent, (await change).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await login!).StatusCode);
    }

    private async Task WaitForBlockedRequestsAsync(int expected, Task? operation = null)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        var timeout = Stopwatch.StartNew();
        while (timeout.Elapsed < TimeSpan.FromSeconds(10))
        {
            if (operation?.IsCompleted == true) return;
            await using var query = new NpgsqlCommand("""
                SELECT count(*) FROM pg_stat_activity
                WHERE application_name = @name AND wait_event_type = 'Lock'
                """, connection);
            query.Parameters.AddWithValue("name", _applicationName);
            if (Convert.ToInt32(await query.ExecuteScalarAsync()) >= expected) return;
            await Task.Delay(20);
        }
        Assert.Fail($"Expected {expected} requests to reach the database lock.");
    }

    private async Task<JsonElement> LoginAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            login = _username,
            password = InitialPassword
        });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<string> RequestResetAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = Email });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("developmentResetToken").GetString()!;
    }

    private Task<HttpResponseMessage> ResetPasswordAsync(string token) =>
        _client.PostAsJsonAsync("/api/v1/auth/reset-password", new { token, newPassword = ChangedPassword });

    private Task<HttpResponseMessage> ChangePasswordAsync(JsonElement session) =>
        AuthorizedAsync(HttpMethod.Post, "/api/v1/auth/change-password", session,
            new { currentPassword = InitialPassword, newPassword = ChangedPassword });

    private async Task<HttpResponseMessage> AuthorizedAsync(
        HttpMethod method, string path, JsonElement session, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
            session.GetProperty("accessToken").GetString());
        if (body is not null) request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }

    public async Task DisposeAsync()
    {
        _gate.Release();
        _client?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        await _baseFactory.DisposeAsync();
        if (_userId is null) return;
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var cleanup = new NpgsqlCommand("""
            DELETE FROM integration.outbox_events WHERE aggregate_id = @user_id;
            DELETE FROM audit.security_events WHERE user_id = @user_id;
            DELETE FROM identity.users WHERE id = @user_id;
            """, connection, transaction);
        cleanup.Parameters.AddWithValue("user_id", _userId.Value);
        await cleanup.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }

    public sealed class PasswordCheckGate
    {
        private int _armed;
        private readonly ManualResetEventSlim _release = new(true);
        public TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Arm()
        {
            _release.Reset();
            Interlocked.Exchange(ref _armed, 1);
        }
        public void Release() => _release.Set();
        public void PauseOnce()
        {
            if (Interlocked.Exchange(ref _armed, 0) == 0) return;
            Reached.TrySetResult();
            if (!_release.Wait(TimeSpan.FromSeconds(15))) throw new TimeoutException("Password check was not released.");
        }
    }

    public sealed class PausingPasswordHasher<TUser>(PasswordCheckGate gate) : IPasswordHasher<TUser>
        where TUser : class
    {
        private readonly PasswordHasher<TUser> _inner = new();
        public string HashPassword(TUser user, string password)
        {
            var result = _inner.HashPassword(user, password);
            gate.PauseOnce();
            return result;
        }
        public PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword)
        {
            var result = _inner.VerifyHashedPassword(user, hashedPassword, providedPassword);
            gate.PauseOnce();
            return result;
        }
    }
}
