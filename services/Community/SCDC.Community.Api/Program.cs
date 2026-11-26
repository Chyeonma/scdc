using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi;
using SCDC.Api.Authentication;
using SCDC.Api.Errors;
using SCDC.Api.Observability;
using SCDC.Api.OpenApi;
using SCDC.BuildingBlocks.Infrastructure.Transport;
using SCDC.Community.Api.Preparation;
using SCDC.Contracts.Identity;
using SCDC.Contracts.Messaging;
using SCDC.Modules.Community;
using SCDC.Modules.Community.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddCommunitySecretFiles();
builder.AddScdcTelemetry("scdc-community");
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddConsumerAuthentication(builder.Configuration);
builder.Services.AddRemoteIdentity(builder.Configuration);
// Phase 1 never runs recovery work against a live owner or acknowledges a remote operation.
builder.Services.AddCommunityModule(builder.Configuration, builder.Environment.EnvironmentName, runWorkers: false);
builder.Services.AddSingleton<CommunityDatabaseReadiness>();
builder.Services.AddKeyedSingleton<IChannelProvisioning, PendingMessaging>(AccessOwner.Community);
builder.Services.AddSingleton<SCDC.Contracts.Community.ICommunityFenceOwner, PendingMessaging>();
builder.Services.AddControllers();
builder.Services.AddApiProblemDetails();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SCDC Community API",
        Version = "v1",
        Description = "Community preparation host. Business traffic and recovery workers remain disabled until service integrations are verified."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT"
    });
    options.OperationFilter<AuthenticationOperationFilter>();
    options.OperationFilter<CommunityServerViewOperationFilter>();
    options.SchemaFilter<CommunityResponseSchemaFilter>();
});

var app = builder.Build();
app.UseScdcRequestTelemetry();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRouting();
app.Use(async (context, next) =>
{
    // Inspect the selected action, rather than trusting the spelling of the URL.
    // This also blocks encoded routes and any future Community controller action.
    var action = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
    if (action is not null)
    {
        await CommunityPreparation.WriteUnavailableAsync(context);
        return;
    }
    await next(context);
});
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options => options.RouteTemplate = "swagger/community/{documentName}/swagger.json");
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger/community";
        options.SwaggerEndpoint("/swagger/community/v1/swagger.json", "SCDC Community API v1");
    });
}
app.MapGet("/health/live", () => Results.Ok(new
{
    service = "community", status = "alive", mode = "preparation", workersEnabled = false
}));
app.MapGet("/health/database", async (CommunityDatabaseReadiness database, CancellationToken ct) =>
    await database.IsReadyAsync(ct)
        ? Results.Ok(new { status = "ready" })
        : Results.Json(new { status = "unavailable" }, statusCode: 503));
app.MapGet("/health/ready", CommunityPreparation.WriteUnavailableAsync);
app.MapControllers();
await app.RunAsync();

public partial class CommunityProgram;
