using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using SCDC.Api.Errors;
using SCDC.Api.OpenApi;
using SCDC.Modules.Community;
using SCDC.Modules.Identity;
using SCDC.Modules.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddCommunityModule(builder.Configuration);
builder.Services.AddMessagingModule(builder.Configuration);

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

await app.RunAsync();

public partial class Program;
