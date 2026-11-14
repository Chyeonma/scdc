using SCDC.Sample.Application.Health;
using SCDC.Sample.Infrastructure;
using SCDC.Sample.Api.Observability;
using SCDC.Sample.Api.Authentication;

var builder = WebApplication.CreateBuilder(args);
builder.AddScdcTelemetry("scdc-sample");
builder.AddSampleAuthentication();
builder.Services.AddSingleton<ReadServiceHealth>();
builder.Services.AddSampleInfrastructure();

var app = builder.Build();
app.UseScdcRequestTelemetry();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", (ReadServiceHealth query, HttpContext context) =>
{
    context.Response.Headers.CacheControl = "no-store";
    var health = query.Execute();
    return Results.Json(health, statusCode: health.Status == "healthy"
        ? StatusCodes.Status200OK
        : StatusCodes.Status503ServiceUnavailable);
});
app.MapGet("/sample/protected", (HttpContext context) =>
{
    context.Response.Headers.CacheControl = "no-store";
    return Results.Ok(new { service = "sample-service", userId = context.User.FindFirst("sub")!.Value });
}).RequireAuthorization();
app.Run();

public partial class Program;
