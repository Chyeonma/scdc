using Microsoft.AspNetCore.Mvc;

namespace SCDC.Community.Api.Errors;

public sealed class ApiProblemDetails : ProblemDetails
{
    public required string ErrorCode { get; init; }

    public required string TraceId { get; init; }
}

public sealed class ApiValidationProblemDetails(
    IDictionary<string, string[]> errors) : HttpValidationProblemDetails(errors)
{
    public required string ErrorCode { get; init; }

    public required string TraceId { get; init; }
}
