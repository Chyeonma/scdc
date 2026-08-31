namespace ChatService.Common.Errors;

internal static class ProblemTypes
{
    private const string BaseUrl = "https://scdc.dev/problems";

    public const string ValidationError = $"{BaseUrl}/validation-error";
    public const string AuthenticationFailed = $"{BaseUrl}/authentication-failed";
    public const string Forbidden = $"{BaseUrl}/forbidden";
    public const string NotFound = $"{BaseUrl}/not-found";
    public const string Conflict = $"{BaseUrl}/conflict";
    public const string RateLimitExceeded = $"{BaseUrl}/rate-limit-exceeded";
    public const string InternalServerError = $"{BaseUrl}/internal-server-error";
}
