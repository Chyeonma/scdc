namespace SCDC.BuildingBlocks.Application.Results;

public record Error(string Code, string Description, ErrorType Type)
{
    public static Error None { get; } = new(string.Empty, string.Empty, ErrorType.None);

    public static Error Validation(string code, string description) =>
        new(code, description, ErrorType.Validation);

    public static Error Unauthorized(string code, string description) =>
        new(code, description, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string description) =>
        new(code, description, ErrorType.Forbidden);

    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);

    public static Error TooManyRequests(string code, string description) =>
        new(code, description, ErrorType.TooManyRequests);

    public static Error ServiceUnavailable(string code, string description) =>
        new(code, description, ErrorType.ServiceUnavailable);
}
