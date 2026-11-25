using Microsoft.AspNetCore.Mvc;
using SCDC.Identity.Api.Observability;
using Microsoft.OpenApi;
using SCDC.Identity.Api.Errors;
using SCDC.Identity.Api.OpenApi;
using SCDC.Identity.Api.InternalRpc;
using SCDC.Modules.Identity.Infrastructure.Transport;
using SCDC.Modules.Identity;



var builder = WebApplication.CreateBuilder(args);
if (args.Contains("--initialize-jwt-keys", StringComparer.Ordinal))
{
    IdentityModule.InitializeSigningKeys(builder.Configuration);
    return;
}
builder.AddScdcTelemetry("scdc-identity");
// Query credentials in WebSocket handshakes must never reach request-start diagnostics.
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddRemoteFenceOwners(builder.Configuration);
builder.Services.AddGrpc(options => { options.MaxReceiveMessageSize = 128 * 1024; options.Interceptors.Add<RpcPeerGuard>(); });
RpcTransport.ConfigureServer(builder);


builder.Services.AddControllers();
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
        Title = "SCDC Identity API",
        Version = "v1",
        Description = "Identity accounts, sessions and access authority."
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

if (args.Contains("--initialize-user-search-keys", StringComparer.Ordinal))
{
    await app.Services.InitializeUserSearchKeysAsync();
    return;
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options => options.RouteTemplate = "swagger/identity/{documentName}/swagger.json");
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger/identity";
        options.SwaggerEndpoint("/swagger/identity/v1/swagger.json", "SCDC Identity API v1");
        options.DocumentTitle = "SCDC Identity API";
        options.DisplayRequestDuration();
    });

    app.MapGet("/", () => Results.Redirect("/swagger/identity"))
        .ExcludeFromDescription();
}

app.MapControllers();
app.MapGrpcService<IdentityAccessEndpoint>();
app.MapGrpcService<IdentityDirectoryEndpoint>();

await app.RunAsync();

public partial class IdentityProgram;
