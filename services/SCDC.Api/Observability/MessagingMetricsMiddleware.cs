using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Controllers;
using SCDC.Modules.Messaging.Infrastructure;

namespace SCDC.Api.Observability;

internal sealed class MessagingMetricsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var action = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
        var operation = action is null ? null : (action.ControllerName, action.ActionName) switch
        {
            ("Messages", "Send") => "send",
            ("Messages", "GetHistory") => "history",
            ("Messages", "Search") => "search",
            ("Spaces", "List") => "unread",
            _ => null
        };
        if (operation is null)
        {
            await next(context);
            return;
        }

        var started = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        catch
        {
            MessagingTelemetry.RecordRequest(operation, 500, Stopwatch.GetElapsedTime(started).TotalSeconds);
            throw;
        }

        MessagingTelemetry.RecordRequest(operation, context.Response.StatusCode,
            Stopwatch.GetElapsedTime(started).TotalSeconds);
    }
}
