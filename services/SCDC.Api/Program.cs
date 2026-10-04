using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SCDC.Api.Errors;
using SCDC.Api.Observability;
using SCDC.Api.OpenApi;
using SCDC.Modules.Community;
using SCDC.Modules.Identity;
using SCDC.Modules.Messaging;
using SCDC.Modules.Messaging.Hubs;
using SCDC.Modules.Messaging.Infrastructure;
using SCDC.Modules.Moderation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddCommunityModule(builder.Configuration);
builder.Services.AddMessagingModule(builder.Configuration);
builder.Services.AddModerationModule(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddApiProblemDetails();

var otlpEndpoint = builder.Configuration["Observability:OtlpEndpoint"];
if (!string.IsNullOrWhiteSpace(otlpEndpoint))
{
    if (!Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpoint)
        || endpoint.Scheme is not ("http" or "https"))
        throw new InvalidOperationException("Observability:OtlpEndpoint must be an absolute HTTP(S) URL.");

    builder.Services.AddOpenTelemetry()
        .WithMetrics(metrics => metrics
            .AddMeter(MessagingTelemetry.MeterName, "Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Http.Connections")
            .AddOtlpExporter(options => options.Endpoint = endpoint))
        .WithTracing(traces => traces
            .AddSource(MessagingTelemetry.ActivitySourceName)
            .AddOtlpExporter(options => options.Endpoint = endpoint));
}

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SCDC API",
        Version = "v1",
        Description = "SCDC modular monolith. Identity v1 is active; Community and Messaging are at foundation stage."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste the access token returned by POST /api/v1/auth/login."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document, null)] = []
    });
    options.OperationFilter<AuthenticationOperationFilter>();
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRouting();
app.UseMiddleware<MessagingMetricsMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SCDC API v1");
        options.DocumentTitle = "SCDC API";
        options.DisplayRequestDuration();
    });

    app.MapGet("/", () => Results.Redirect("/swagger"))
        .ExcludeFromDescription();
}

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

await app.RunAsync();

public partial class Program;
