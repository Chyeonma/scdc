using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SCDC.Api.Tests.Infrastructure;

namespace SCDC.Api.Tests.Identity;

public sealed class IdentityV1FlowTests(SCDCWebApplicationFactory factory)
    : IClassFixture<SCDCWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Identity_v1_supports_the_complete_password_account_lifecycle()
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var username = $"it_{suffix}";
        var email = $"{username}@example.test";
        const string initialPassword = "Initial123";
        const string newPassword = "Changed456";
        const string finalPassword = "Final7890";
        Guid? userId = null;

        try
        {
            var registration = await _client.PostAsJsonAsync("/api/v1/auth/register", new
            {
                username,
                displayName = "Identity Integration Test",
                email,
                password = initialPassword
            });
            Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
            var registrationBody = await registration.Content.ReadFromJsonAsync<JsonElement>();
            userId = registrationBody.GetProperty("userId").GetGuid();
            var verificationToken = registrationBody
                .GetProperty("developmentVerificationToken")
                .GetString();
            Assert.False(string.IsNullOrWhiteSpace(verificationToken));

            var loginBeforeVerification = await LoginAsync(username, initialPassword);
            Assert.Equal(HttpStatusCode.Forbidden, loginBeforeVerification.StatusCode);

            var verify = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", new
            {
                token = verificationToken
            });
            Assert.Equal(HttpStatusCode.NoContent, verify.StatusCode);

            var firstLogin = await LoginAsync(username, initialPassword);
            Assert.Equal(HttpStatusCode.OK, firstLogin.StatusCode);
            var firstSession = await ReadAuthAsync(firstLogin);

            var me = await SendAuthorizedAsync(HttpMethod.Get, "/api/v1/users/me", firstSession.AccessToken);
            Assert.Equal(HttpStatusCode.OK, me.StatusCode);
            var meBody = await me.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(username, meBody.GetProperty("username").GetString());
            Assert.True(meBody.GetProperty("emailVerified").GetBoolean());

            var updateProfile = await SendAuthorizedAsync(
                HttpMethod.Patch,
                "/api/v1/users/me",
                firstSession.AccessToken,
                new
                {
                    displayName = "Updated Identity User",
                    bio = "Identity v1 integration test",
                    locale = "vi-VN",
                    timezone = "Asia/Ho_Chi_Minh"
                });
            Assert.Equal(HttpStatusCode.OK, updateProfile.StatusCode);

            var refresh = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new
            {
                refreshToken = firstSession.RefreshToken
            });
            Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
            var rotatedSession = await ReadAuthAsync(refresh);

            var reuse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new
            {
                refreshToken = firstSession.RefreshToken
            });
            Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

            var revokedAccess = await SendAuthorizedAsync(
                HttpMethod.Get,
                "/api/v1/users/me",
                rotatedSession.AccessToken);
            Assert.Equal(HttpStatusCode.Unauthorized, revokedAccess.StatusCode);

            var secondLogin = await LoginAsync(username, initialPassword);
            Assert.Equal(HttpStatusCode.OK, secondLogin.StatusCode);
            var secondSession = await ReadAuthAsync(secondLogin);

            var sessions = await SendAuthorizedAsync(
                HttpMethod.Get,
                "/api/v1/auth/sessions",
                secondSession.AccessToken);
            Assert.Equal(HttpStatusCode.OK, sessions.StatusCode);
            var sessionsBody = await sessions.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains(sessionsBody.EnumerateArray(), item => item.GetProperty("isCurrent").GetBoolean());

            var forgotPassword = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new
            {
                email
            });
            Assert.Equal(HttpStatusCode.Accepted, forgotPassword.StatusCode);
            var forgotBody = await forgotPassword.Content.ReadFromJsonAsync<JsonElement>();
            var resetToken = forgotBody.GetProperty("developmentResetToken").GetString();
            Assert.False(string.IsNullOrWhiteSpace(resetToken));

            var resetPassword = await _client.PostAsJsonAsync("/api/v1/auth/reset-password", new
            {
                token = resetToken,
                newPassword
            });
            Assert.Equal(HttpStatusCode.NoContent, resetPassword.StatusCode);

            var accessAfterReset = await SendAuthorizedAsync(
                HttpMethod.Get,
                "/api/v1/users/me",
                secondSession.AccessToken);
            Assert.Equal(HttpStatusCode.Unauthorized, accessAfterReset.StatusCode);

            var oldPasswordLogin = await LoginAsync(username, initialPassword);
            Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordLogin.StatusCode);

            var finalLogin = await LoginAsync(username, newPassword);
            Assert.Equal(HttpStatusCode.OK, finalLogin.StatusCode);
            var finalSession = await ReadAuthAsync(finalLogin);

            var changePassword = await SendAuthorizedAsync(
                HttpMethod.Post,
                "/api/v1/auth/change-password",
                finalSession.AccessToken,
                new
                {
                    currentPassword = newPassword,
                    newPassword = finalPassword
                });
            Assert.Equal(HttpStatusCode.NoContent, changePassword.StatusCode);

            var accessAfterChange = await SendAuthorizedAsync(
                HttpMethod.Get,
                "/api/v1/users/me",
                finalSession.AccessToken);
            Assert.Equal(HttpStatusCode.Unauthorized, accessAfterChange.StatusCode);

            var firstActiveLogin = await LoginAsync(username, finalPassword);
            var secondActiveLogin = await LoginAsync(username, finalPassword);
            Assert.Equal(HttpStatusCode.OK, firstActiveLogin.StatusCode);
            Assert.Equal(HttpStatusCode.OK, secondActiveLogin.StatusCode);
            var firstActiveSession = await ReadAuthAsync(firstActiveLogin);
            var secondActiveSession = await ReadAuthAsync(secondActiveLogin);

            var activeSessions = await SendAuthorizedAsync(
                HttpMethod.Get,
                "/api/v1/auth/sessions",
                firstActiveSession.AccessToken);
            var activeSessionsBody = await activeSessions.Content.ReadFromJsonAsync<JsonElement>();
            var otherSessionId = activeSessionsBody
                .EnumerateArray()
                .Single(item => !item.GetProperty("isCurrent").GetBoolean())
                .GetProperty("id")
                .GetGuid();

            var revokeOtherSession = await SendAuthorizedAsync(
                HttpMethod.Delete,
                $"/api/v1/auth/sessions/{otherSessionId}",
                firstActiveSession.AccessToken);
            Assert.Equal(HttpStatusCode.NoContent, revokeOtherSession.StatusCode);

            var revokedOtherAccess = await SendAuthorizedAsync(
                HttpMethod.Get,
                "/api/v1/users/me",
                secondActiveSession.AccessToken);
            Assert.Equal(HttpStatusCode.Unauthorized, revokedOtherAccess.StatusCode);

            var logoutAll = await SendAuthorizedAsync(
                HttpMethod.Post,
                "/api/v1/auth/logout-all",
                firstActiveSession.AccessToken);
            Assert.Equal(HttpStatusCode.NoContent, logoutAll.StatusCode);

            var accessAfterLogoutAll = await SendAuthorizedAsync(
                HttpMethod.Get,
                "/api/v1/users/me",
                firstActiveSession.AccessToken);
            Assert.Equal(HttpStatusCode.Unauthorized, accessAfterLogoutAll.StatusCode);

            var logoutLogin = await LoginAsync(username, finalPassword);
            Assert.Equal(HttpStatusCode.OK, logoutLogin.StatusCode);
            var logoutSession = await ReadAuthAsync(logoutLogin);

            var logout = await _client.PostAsJsonAsync("/api/v1/auth/logout", new
            {
                refreshToken = logoutSession.RefreshToken
            });
            Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

            var accessAfterLogout = await SendAuthorizedAsync(
                HttpMethod.Get,
                "/api/v1/users/me",
                logoutSession.AccessToken);
            Assert.Equal(HttpStatusCode.Unauthorized, accessAfterLogout.StatusCode);
        }
        finally
        {
            await CleanupAsync(userId, username);
        }
    }

    private Task<HttpResponseMessage> LoginAsync(string login, string password) =>
        _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            login,
            password,
            deviceName = "Integration test"
        });

    private async Task<HttpResponseMessage> SendAuthorizedAsync(
        HttpMethod method,
        string path,
        string accessToken,
        object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }

    private static async Task<AuthTokens> ReadAuthAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new AuthTokens(
            body.GetProperty("accessToken").GetString()!,
            body.GetProperty("refreshToken").GetString()!);
    }

    private async Task CleanupAsync(Guid? userId, string username)
    {
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Test database connection is missing.");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var resolvedUserId = userId;
        if (resolvedUserId is null)
        {
            await using var findCommand = new NpgsqlCommand(
                "SELECT id FROM identity.users WHERE normalized_username = lower(@username)",
                connection);
            findCommand.Parameters.AddWithValue("username", username);
            resolvedUserId = await findCommand.ExecuteScalarAsync() as Guid?;
        }

        if (resolvedUserId is null)
        {
            return;
        }

        await using var transaction = await connection.BeginTransactionAsync();
        await ExecuteDeleteAsync(
            connection,
            transaction,
            "DELETE FROM integration.outbox_events WHERE aggregate_id = @user_id",
            resolvedUserId.Value);
        await ExecuteDeleteAsync(
            connection,
            transaction,
            "DELETE FROM audit.security_events WHERE user_id = @user_id",
            resolvedUserId.Value);
        await ExecuteDeleteAsync(
            connection,
            transaction,
            "DELETE FROM identity.users WHERE id = @user_id",
            resolvedUserId.Value);
        await transaction.CommitAsync();
    }

    private static async Task ExecuteDeleteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        Guid userId)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("user_id", userId);
        await command.ExecuteNonQueryAsync();
    }

    private sealed record AuthTokens(string AccessToken, string RefreshToken);
}
