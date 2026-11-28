using SCDC.BuildingBlocks.Application.Results;

namespace SCDC.Api.Errors;

internal sealed record ApiErrorDescriptor(
    int Status,
    string Code,
    string Title,
    string Type);

internal static class ApiErrorDefaults
{
    private const string ProblemBaseUri = "https://scdc.dev/problems";

    public static ApiErrorDescriptor FromError(Error error) => error.Type switch
    {
        ErrorType.Validation => Create(
            StatusCodes.Status400BadRequest,
            error.Code,
            "Validation failed.",
            "validation"),
        ErrorType.Unauthorized => Create(
            StatusCodes.Status401Unauthorized,
            error.Code,
            "Authentication is required.",
            "unauthorized"),
        ErrorType.Forbidden => Create(
            StatusCodes.Status403Forbidden,
            error.Code,
            "Access is forbidden.",
            "forbidden"),
        ErrorType.NotFound => Create(
            StatusCodes.Status404NotFound,
            error.Code,
            "Resource not found.",
            "not-found"),
        ErrorType.Conflict => Create(
            StatusCodes.Status409Conflict,
            error.Code,
            "A conflict occurred.",
            "conflict"),
        ErrorType.TooManyRequests => Create(
            StatusCodes.Status429TooManyRequests,
            error.Code,
            "Too many requests.",
            "too-many-requests"),
        ErrorType.ServiceUnavailable => Create(
            StatusCodes.Status503ServiceUnavailable,
            error.Code,
            "Service unavailable.",
            "service-unavailable"),
        ErrorType.None => throw new InvalidOperationException(
            "Error.None cannot be converted to an HTTP response."),
        _ => throw new ArgumentOutOfRangeException(nameof(error), error.Type, "Unknown error type.")
    };

    public static ApiErrorDescriptor FromStatusCode(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => Create(statusCode, "Common.BadRequest", "Bad request.", "bad-request"),
        StatusCodes.Status401Unauthorized => Create(statusCode, "Common.Unauthorized", "Authentication is required.", "unauthorized"),
        StatusCodes.Status403Forbidden => Create(statusCode, "Common.Forbidden", "Access is forbidden.", "forbidden"),
        StatusCodes.Status404NotFound => Create(statusCode, "Common.NotFound", "Resource not found.", "not-found"),
        StatusCodes.Status405MethodNotAllowed => Create(statusCode, "Common.MethodNotAllowed", "Method not allowed.", "method-not-allowed"),
        StatusCodes.Status409Conflict => Create(statusCode, "Common.Conflict", "A conflict occurred.", "conflict"),
        StatusCodes.Status415UnsupportedMediaType => Create(statusCode, "Common.UnsupportedMediaType", "Unsupported media type.", "unsupported-media-type"),
        StatusCodes.Status422UnprocessableEntity => Create(statusCode, "Common.UnprocessableEntity", "The request could not be processed.", "unprocessable-entity"),
        StatusCodes.Status429TooManyRequests => Create(statusCode, "Common.TooManyRequests", "Too many requests.", "too-many-requests"),
        StatusCodes.Status500InternalServerError => Unexpected,
        StatusCodes.Status502BadGateway => Create(statusCode, "Common.BadGateway", "Bad gateway.", "bad-gateway"),
        StatusCodes.Status503ServiceUnavailable => Create(statusCode, "Common.ServiceUnavailable", "Service unavailable.", "service-unavailable"),
        StatusCodes.Status504GatewayTimeout => Create(statusCode, "Common.GatewayTimeout", "Gateway timeout.", "gateway-timeout"),
        _ => Create(statusCode, "Common.HttpError", "The request could not be completed.", "http-error")
    };

    public static ApiErrorDescriptor Validation { get; } = Create(
        StatusCodes.Status400BadRequest,
        "Common.ValidationFailed",
        "Validation failed.",
        "validation");

    public static ApiErrorDescriptor Unexpected { get; } = Create(
        StatusCodes.Status500InternalServerError,
        "Common.UnexpectedError",
        "An unexpected error occurred.",
        "internal-server-error");

    private static ApiErrorDescriptor Create(
        int status,
        string code,
        string title,
        string typeSlug) => new(status, code, title, $"{ProblemBaseUri}/{typeSlug}");
}
