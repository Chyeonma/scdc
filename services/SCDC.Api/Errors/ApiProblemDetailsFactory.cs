using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SCDC.Api.Errors;

internal static class ApiProblemDetailsFactory
{
    public static ApiProblemDetails Create(
        HttpContext httpContext,
        ApiErrorDescriptor descriptor,
        string detail) =>
        new()
        {
            Type = descriptor.Type,
            Title = descriptor.Title,
            Status = descriptor.Status,
            Detail = detail,
            Instance = httpContext.Request.Path.Value,
            ErrorCode = descriptor.Code,
            TraceId = GetTraceId(httpContext)
        };

    public static ApiValidationProblemDetails CreateValidation(
        HttpContext httpContext,
        IDictionary<string, string[]> errors,
        string? code = null,
        string? detail = null)
    {
        var descriptor = ApiErrorDefaults.Validation;

        return new ApiValidationProblemDetails(errors)
        {
            Type = descriptor.Type,
            Title = descriptor.Title,
            Status = descriptor.Status,
            Detail = detail ?? "One or more validation errors occurred.",
            Instance = httpContext.Request.Path.Value,
            ErrorCode = code ?? descriptor.Code,
            TraceId = GetTraceId(httpContext)
        };
    }

    public static void Enrich(ProblemDetails problemDetails, HttpContext httpContext)
    {
        var status = problemDetails.Status ?? httpContext.Response.StatusCode;
        var descriptor = ApiErrorDefaults.FromStatusCode(status);

        problemDetails.Status ??= status;
        problemDetails.Type ??= descriptor.Type;
        problemDetails.Title ??= descriptor.Title;
        problemDetails.Instance ??= httpContext.Request.Path.Value;

        if (problemDetails is not ApiProblemDetails and not ApiValidationProblemDetails)
        {
            problemDetails.Extensions.TryAdd("errorCode", descriptor.Code);
            problemDetails.Extensions.TryAdd("traceId", GetTraceId(httpContext));
        }
    }

    private static string GetTraceId(HttpContext httpContext) =>
        Activity.Current?.Id ?? httpContext.TraceIdentifier;
}
