using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using SCDC.Media.Api;
using SCDC.Media.Application;
using SCDC.Media.Domain;
using SCDC.Media.Infrastructure;
using SCDC.Shared.Core.Authentication;

var builder = WebApplication.CreateBuilder(args);
// Request URLs may carry temporary SFU/SignalR credentials. Never emit them in logs.
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.None);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<MediaRegistry>();
builder.Services.AddSingleton<LiveKitProxy>();
builder.Services.AddSingleton<ProviderGate>();
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(builder.Configuration.GetConnectionString("Media") ?? throw new InvalidOperationException("Media database required.")));
builder.Services.AddScoped<ICallStore, CallStore>();
builder.Services.AddScoped<CallService>();
builder.Services.AddHttpClient<IIdentityAuthority, IdentityAuthority>(client => {
    client.BaseAddress = new Uri(builder.Configuration["Identity:BaseUrl"]!); client.Timeout = TimeSpan.FromSeconds(2);
});
builder.Services.AddHttpClient<IMediaProvider, LiveKitProvider>(client => {
    client.BaseAddress = new Uri(builder.Configuration["LiveKit:BaseUrl"]!); client.Timeout = TimeSpan.FromSeconds(2);
});
builder.Services.AddHttpClient("signaling", client => client.Timeout = TimeSpan.FromSeconds(5));
if ((builder.Configuration["LiveKit:ApiSecret"]?.Length ?? 0) < 32 || string.IsNullOrWhiteSpace(builder.Configuration["LiveKit:ApiKey"]))
    throw new InvalidOperationException("Configure a LiveKit key and a secret of at least 32 characters.");
var trust = builder.Configuration.GetSection(JwtTrustOptions.SectionName).Get<JwtTrustOptions>() ?? new();
var keys = trust.LoadPublicKeys().Select(k => new RsaSecurityKey(k.Parameters) { KeyId = k.KeyId }).ToArray();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => {
    options.MapInboundClaims = false; options.IncludeErrorDetails = false;
    options.TokenValidationParameters = new() {
        ValidateIssuer = true, ValidIssuer = trust.Issuer, ValidateAudience = true, ValidAudience = trust.Audience,
        ValidateIssuerSigningKey = true, IssuerSigningKeys = keys, ValidAlgorithms = [JwtTrustOptions.Algorithm],
        TryAllIssuerSigningKeys = false, ValidateLifetime = true, RequireExpirationTime = true, RequireSignedTokens = true,
        ClockSkew = TimeSpan.FromSeconds(trust.ClockSkewSeconds)
    };
    options.Events = new() {
        OnMessageReceived = context => {
            if (context.Request.Method == "GET" && context.Request.Path == "/hubs/media")
                context.Token = context.Request.Query["access_token"];
            return Task.CompletedTask;
        },
        OnTokenValidated = context => {
            if (!new[] { "sub", "sid", "sst" }.All(c => Guid.TryParse(context.Principal?.FindFirst(c)?.Value, out _))) context.Fail("Invalid identity claims.");
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization(); builder.Services.AddSignalR(options => options.EnableDetailedErrors = false);
builder.Services.AddSingleton<IUserIdProvider, MediaUserIdProvider>(); builder.Services.AddHostedService<CallWorker>();
var app = builder.Build();
app.Use(async (context, next) => {
    try { await next(); }
    catch (Exception e) when (!context.Response.HasStarted) {
        var problem = e as MediaProblem;
        context.Response.StatusCode = problem?.Status ?? (e is NpgsqlException or HttpRequestException or OperationCanceledException ? 503 : 500);
        context.Response.Headers.CacheControl = "no-store";
        await context.Response.WriteAsJsonAsync(new { code = problem?.Code ?? "Media.Unavailable", detail = problem?.Message ?? "The call service is temporarily unavailable." });
    }
});
app.UseWebSockets(); app.UseAuthentication(); app.UseAuthorization();
app.MapGet("/api/v1/media/health", () => Results.Ok(new { status = "healthy" }));
app.MapHub<MediaHub>("/hubs/media", options => options.CloseOnAuthenticationExpiration = true);
app.Map("/livekit/{**path}", (HttpContext context, LiveKitProxy proxy) => proxy.Handle(context));
var api = app.MapGroup("/api/v1/media").RequireAuthorization();
static MediaActor Actor(HttpContext http, Guid instance, MediaRegistry registry) => registry.Require(
    Guid.Parse(http.User.FindFirstValue("sub")!), Guid.Parse(http.User.FindFirstValue("sid")!), instance);
api.MapGet("/state", async (Guid clientInstanceId, HttpContext http, MediaRegistry registry, CallService service, ICallStore store, CancellationToken ct) => {
    http.Response.Headers.CacheControl = "no-store";
    var actor = Actor(http, clientInstanceId, registry); await service.Check(actor, ct);
    return Results.Ok(new { call = (await store.CurrentAsync(actor.UserId, ct))?.Snapshot(actor) });
});
api.MapGet("/calls/{id:guid}", async (Guid id, Guid clientInstanceId, HttpContext http, MediaRegistry registry, CallService service, ICallStore store, CancellationToken ct) => {
    http.Response.Headers.CacheControl = "no-store";
    var actor = Actor(http, clientInstanceId, registry); await service.Check(actor, ct); var call = await store.GetAsync(id, ct);
    if (!call.Contains(actor.UserId)) throw new MediaProblem(404, "Media.NotFound", "Call unavailable.");
    return Results.Ok(new { call = call.Snapshot(actor) });
});
api.MapPost("/calls", async (CreateCall body, HttpContext http, MediaRegistry registry, CallService service, CancellationToken ct) => {
    var actor = Actor(http, body.ClientInstanceId, registry);
    return Results.Ok(new { call = (await service.Create(actor, body.PeerUserId, body.ClientOperationId, ct)).Snapshot(actor) });
});
api.MapPost("/calls/{id:guid}/connection", async (Guid id, CallConnection body, HttpContext http, MediaRegistry registry, CallService service, ICallStore store, IMediaProvider provider, CancellationToken ct) => {
    http.Response.Headers.CacheControl = "no-store";
    var actor = Actor(http, body.ClientInstanceId, registry); var call = await store.GetAsync(id, ct);
    if (!call.Contains(actor.UserId)) throw new MediaProblem(404, "Media.NotFound", "Call unavailable.");
    if (!call.Owns(actor)) throw new MediaProblem(409, "Media.NotOwner", "This tab does not own the call.");
    await service.CheckCall(call, ct);
    return Results.Ok(new { callId = call.Id, serverUrl = "/livekit", token = provider.Token(call, actor) });
});
api.MapPost("/calls/{id:guid}/{action}", async (Guid id, string action, CallAction body, HttpContext http, MediaRegistry registry, CallService service, CancellationToken ct) => {
    if (action is not ("accept" or "reject" or "cancel" or "end")) throw new MediaProblem(404, "Media.NotFound", "Unknown call action.");
    var actor = Actor(http, body.ClientInstanceId, registry);
    return Results.Ok(new { call = (await service.Act(id, actor, action, body.ExpectedVersion, body.ClientOperationId, ct)).Snapshot(actor) });
});
using (var scope = app.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<ICallStore>().RecoverAsync(CancellationToken.None);
app.Run();
public sealed record CreateCall(Guid PeerUserId, Guid ClientInstanceId, Guid ClientOperationId);
public sealed record CallAction(string ExpectedVersion, Guid ClientInstanceId, Guid ClientOperationId);
public sealed record CallConnection(Guid ClientInstanceId);
// Serializes room creation against cleanup after an end/revocation race.
public sealed class ProviderGate { public SemaphoreSlim Lock { get; } = new(1, 1); }
