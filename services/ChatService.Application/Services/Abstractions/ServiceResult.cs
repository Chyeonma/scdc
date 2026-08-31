namespace ChatService.Services.Abstractions;

public enum ServiceStatus
{
    Ok,
    Created,
    NoContent,
    ValidationError,
    AuthenticationFailed,
    Forbidden,
    NotFound,
    Conflict,
    TooManyRequests
}

public sealed record ServiceResult<T>(
    ServiceStatus Status,
    T? Value = default,
    string? Detail = null,
    IReadOnlyDictionary<string, string[]>? Errors = null)
{
    public bool IsSuccess => Status is ServiceStatus.Ok or ServiceStatus.Created or ServiceStatus.NoContent;

    public static ServiceResult<T> Ok(T value) => new(ServiceStatus.Ok, value);
    public static ServiceResult<T> Created(T value) => new(ServiceStatus.Created, value);
    public static ServiceResult<T> NoContent() => new(ServiceStatus.NoContent);
    public static ServiceResult<T> AuthenticationFailed(string detail) =>
        new(ServiceStatus.AuthenticationFailed, Detail: detail);
    public static ServiceResult<T> Forbidden(string detail) => new(ServiceStatus.Forbidden, Detail: detail);
    public static ServiceResult<T> NotFound(string detail) => new(ServiceStatus.NotFound, Detail: detail);
    public static ServiceResult<T> Conflict(string detail) => new(ServiceStatus.Conflict, Detail: detail);
    public static ServiceResult<T> TooManyRequests(string detail) =>
        new(ServiceStatus.TooManyRequests, Detail: detail);
    public static ServiceResult<T> Validation(string field, string message) =>
        new(
            ServiceStatus.ValidationError,
            Errors: new Dictionary<string, string[]> { [field] = [message] });
}
