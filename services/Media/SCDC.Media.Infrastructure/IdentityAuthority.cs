using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SCDC.Media.Application;
using SCDC.Media.Domain;

namespace SCDC.Media.Infrastructure;

// Temporary HTTP adapter to the current Identity host. Replace behind this interface
// with the agreed gRPC/session authority when STEP-2/3 makes it available.
public sealed class IdentityAuthority(HttpClient client) : IIdentityAuthority
{
    public async Task<PublicUser> CheckAsync(string token, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/users/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await client.SendAsync(request, ct);
            if ((int)response.StatusCode is 401 or 403)
                throw new MediaProblem(401, "Media.SessionInvalid", "Sign in again.");
            if (!response.IsSuccessStatusCode) throw new MediaProblem(503, "Media.AuthorityUnavailable", "Cannot confirm account access.");
            var user = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            if (user.GetProperty("status").GetString() != "active" || !user.GetProperty("emailVerified").GetBoolean())
                throw new MediaProblem(403, "Media.AccountDenied", "A verified active account is required.");
            return new(user.GetProperty("id").GetGuid(), user.GetProperty("username").GetString()!, user.GetProperty("displayName").GetString()!);
        }
        catch (HttpRequestException) { throw new MediaProblem(503, "Media.AuthorityUnavailable", "Identity is unavailable."); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new MediaProblem(503, "Media.AuthorityUnavailable", "Identity timed out."); }
    }
}
