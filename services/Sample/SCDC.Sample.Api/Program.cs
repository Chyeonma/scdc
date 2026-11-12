using SCDC.Sample.Application.Health;
using SCDC.Sample.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ReadServiceHealth>();
builder.Services.AddSampleInfrastructure();

var app = builder.Build();
app.MapGet("/health", (ReadServiceHealth query, HttpContext context) =>
{
    context.Response.Headers.CacheControl = "no-store";
    var health = query.Execute();
    return Results.Json(health, statusCode: health.Status == "healthy"
        ? StatusCodes.Status200OK
        : StatusCodes.Status503ServiceUnavailable);
});
app.Run();
