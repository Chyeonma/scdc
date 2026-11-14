using System.Diagnostics;
using Microsoft.AspNetCore.Routing;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SCDC.Sample.Api.Observability;

// Framework integration stays in each API, as required by D45.
internal static class ScdcTelemetry
{
    private const string LogCategory = "SCDC.Telemetry";

    public static void AddScdcTelemetry(this WebApplicationBuilder builder, string serviceName)
    {
        builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
        if (!builder.Configuration.GetValue<bool>("Telemetry:Enabled")) return;
        var endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
            ?? throw new InvalidOperationException("OTEL_EXPORTER_OTLP_ENDPOINT is required when telemetry is enabled.");
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new InvalidOperationException("The OTLP endpoint must be an absolute HTTP(S) URL.");

        void Export(OtlpExporterOptions options, string signal)
        {
            options.Protocol = OtlpExportProtocol.HttpProtobuf;
            options.Endpoint = new Uri(endpoint.TrimEnd('/') + "/v1/" + signal);
            options.TimeoutMilliseconds = 1000;
        }

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .SetSampler(new AlwaysOnSampler())
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = false;
                    options.Filter = context => !context.Request.Path.StartsWithSegments("/hubs");
                })
                .AddProcessor(new SafeHttpTags())
                .AddOtlpExporter(options => Export(options, "traces")))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddView("http.server.request.duration", new ExplicitBucketHistogramConfiguration
                {
                    TagKeys = ["http.request.method", "http.response.status_code", "http.route"]
                })
                .AddOtlpExporter(options => Export(options, "metrics")));

        // Export only our bounded, structured HTTP summary, never arbitrary exceptions or module logs.
        builder.Logging.AddFilter<OpenTelemetryLoggerProvider>((category, _) => category == LogCategory);
        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeScopes = false;
            options.IncludeFormattedMessage = true;
            options.ParseStateValues = true;
            options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
            options.AddOtlpExporter(exporter => Export(exporter, "logs"));
        });
    }

    public static void UseScdcRequestTelemetry(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Telemetry:Enabled")) return;
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(LogCategory);
        app.Use(async (context, next) =>
        {
            var started = Stopwatch.GetTimestamp();
            var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "(unmatched)";
            var status = 500;
            try
            {
                await next(context);
                status = context.Response.StatusCode;
            }
            finally
            {
                Activity.Current?.SetTag("http.route", route);
                // No raw path, URL, query, header, body, user or exception message.
                logger.Log(status >= 500 ? LogLevel.Error : LogLevel.Information,
                    "HTTP {Method} {Route} {StatusCode} in {DurationMs} ms",
                    context.Request.Method, route, status, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            }
        });
    }

    private sealed class SafeHttpTags : BaseProcessor<Activity>
    {
        private static readonly HashSet<string> Allowed =
            ["http.request.method", "http.response.status_code", "http.route", "error.type"];

        public override void OnEnd(Activity activity)
        {
            foreach (var tag in activity.TagObjects.ToArray())
                if (!Allowed.Contains(tag.Key)) activity.SetTag(tag.Key, null);
            activity.DisplayName = $"{activity.GetTagItem("http.request.method")} {activity.GetTagItem("http.route") ?? "(unmatched)"}";
            activity.SetStatus(activity.Status); // Drop any free-text status description.
        }
    }
}
