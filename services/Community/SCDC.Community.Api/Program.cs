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
using SCDC.Modules.Community.Infrastructure.Transport;
using SCDC.Community.Api;
using SCDC.Api.InternalRpc;
using SCDC.Community.Api.InternalRpc;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddCommunitySecretFiles();
builder.AddScdcTelemetry("scdc-community");
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddConsumerAuthentication(builder.Configuration);
builder.Services.AddRemoteIdentity(builder.Configuration);
var integrated = builder.Configuration.GetValue("CommunityHost:Integrated", false);
builder.Services.AddSingleton<CommunityDatabaseReadiness>();
if (integrated) builder.Services.AddHostedService<CommunityDatabaseStartupGate>();
builder.Services.AddCommunityModule(builder.Configuration, builder.Environment.EnvironmentName, runWorkers: integrated);
if (integrated)
{
    builder.Services.AddRemoteMessaging();
    builder.Services.AddGrpc(options => { options.MaxReceiveMessageSize = 128 * 1024; options.Interceptors.Add<RpcPeerGuard>(); });
    RpcTransport.ConfigureServer(builder);
}
else
{
    builder.Services.AddKeyedSingleton<IChannelProvisioning, PendingMessaging>(AccessOwner.Community);
    builder.Services.AddSingleton<SCDC.Contracts.Community.ICommunityFenceOwner, PendingMessaging>();
}
builder.Services.AddControllers();
builder.Services.AddApiProblemDetails();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SCDC Community API",
        Version = "v1",
        Description = integrated ? "Community API with service-only gRPC/mTLS integrations." : "Community preparation host. Business traffic and recovery workers remain disabled."
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
    if (action is not null && (!integrated || !await context.RequestServices.GetRequiredService<CommunityDatabaseReadiness>().IsReadyAsync(context.RequestAborted)))
    {
        if (integrated) await CommunityPreparation.WriteDependencyUnavailableAsync(context);
        else await CommunityPreparation.WriteUnavailableAsync(context);
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
    service = "community", status = "alive", mode = integrated ? "integrated" : "preparation", workersEnabled = integrated
}));
app.MapGet("/health/database", async (CommunityDatabaseReadiness database, CancellationToken ct) =>
    await database.IsReadyAsync(ct)
        ? Results.Ok(new { status = "ready" })
        : Results.Json(new { status = "unavailable" }, statusCode: 503));
if (integrated)
{
    app.MapGrpcService<CommunityIdentityFenceEndpoint>();
    app.MapGrpcService<ChannelReadinessEndpoint>();
    app.MapGet("/health/ready", async (HttpContext context) =>
    {
        var services = context.RequestServices;
        if (await services.GetRequiredService<CommunityDatabaseReadiness>().IsReadyAsync(context.RequestAborted)
            && await services.GetRequiredService<ISessionStoreReadiness>().IsReadyAsync(context.RequestAborted)
            && await services.GetRequiredService<CommunityDependencyReadiness>().IsReadyAsync(context.RequestAborted))
            await context.Response.WriteAsJsonAsync(new { status = "ready" }, context.RequestAborted);
        else await CommunityPreparation.WriteDependencyUnavailableAsync(context);
    });
}
else app.MapGet("/health/ready", CommunityPreparation.WriteUnavailableAsync);
app.MapControllers();
await app.RunAsync();

public partial class CommunityProgram;
