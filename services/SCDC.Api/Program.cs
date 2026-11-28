using Microsoft.AspNetCore.Mvc;
using SCDC.Api.Observability;
using Microsoft.OpenApi;
using SCDC.Api.Errors;
using SCDC.Api.OpenApi;
using SCDC.Api.Authentication;
using SCDC.Api.InternalRpc;
using SCDC.BuildingBlocks.Infrastructure.Transport;
using SCDC.Modules.Messaging;
using SCDC.Modules.Messaging.Realtime;

var builder = WebApplication.CreateBuilder(args);
// Keep the deployed telemetry identity stable until the Messaging extraction.
builder.AddScdcTelemetry("scdc-monolith");
// Query credentials in WebSocket handshakes must never reach request-start diagnostics.
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddConsumerAuthentication(builder.Configuration);
builder.Services.AddRemoteIdentity(builder.Configuration);
builder.Services.AddGrpc(options => { options.MaxReceiveMessageSize = 128 * 1024; options.Interceptors.Add<RpcPeerGuard>(); });
RpcTransport.ConfigureServer(builder);
if (builder.Configuration.GetValue("Hosting:CommunityEnabled", false))
    throw new InvalidOperationException("This host serves Messaging only. Community rollback requires a verified pre-extraction image.");
builder.Services.AddMessagingModule(builder.Configuration);

var controllers = builder.Services.AddControllers();
builder.Services.AddApiProblemDetails();

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
        Description = "SCDC Messaging host. Direct messaging, text history, edit/delete, outbox and authenticated SignalR; Identity and Community are separate services."
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
app.UseScdcRequestTelemetry();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
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
app.MapGrpcService<MessagingIdentityFenceEndpoint>();
app.MapGrpcService<MessagingControlEndpoint>();
app.MapGrpcService<MessagingCommunityFenceEndpoint>();
app.MapGrpcService<MessagingChannelProvisionEndpoint>();
app.MapHub<ChatHub>("/hubs/chat", options => options.CloseOnAuthenticationExpiration = true);

await app.RunAsync();

public partial class Program;
