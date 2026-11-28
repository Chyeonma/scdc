extern alias IdentityApi;
extern alias CommunityApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Net;

namespace SCDC.Api.Tests.Infrastructure;

public sealed class SCDCWebApplicationFactory : WebApplicationFactory<ApiProgram>
{
    internal static readonly string EmailKeyRingPath = Directory.CreateTempSubdirectory("scdc-email-tests-").FullName;
    private static readonly string CommunityKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    internal static readonly string CommunityKeyRingPath = Path.Combine(Path.GetTempPath(), $"scdc-test-keys-{Guid.NewGuid():N}");

    public static async Task WaitForAccessReadyAsync(HttpClient client, string path = "/api/v1/health/access")
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (true)
        {
            using var response = await client.GetAsync(path, deadline.Token);
            if (response.StatusCode == HttpStatusCode.OK) return;
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            await Task.Delay(50, deadline.Token);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(TestJwtKeys.Configuration().Concat(new Dictionary<string, string?>
        {
            ["Modules:Identity:Email:KeyRingPath"] = EmailKeyRingPath,
            ["Modules:Community:Operations:ActiveKeyId"] = "test",
            ["Modules:Community:Operations:Keys:test"] = CommunityKey,
            ["Modules:Community:KeyRingPath"] = CommunityKeyRingPath
        })));
        builder.ConfigureServices((context, services) =>
        {
            // Component regressions compose owner implementations in TestServer only.
            // Independent-host tests exercise the production gRPC path separately.
            SCDC.Modules.Identity.IdentityModule.AddIdentityModule(services, context.Configuration, configureAuthentication: false);
            SCDC.Modules.Community.CommunityModule.AddCommunityModule(services, context.Configuration, "Development");
            services.AddControllers().AddApplicationPart(typeof(IdentityApi::SCDC.Identity.Api.Controllers.Identity.AuthController).Assembly);
            services.AddControllers().AddApplicationPart(typeof(CommunityApi::SCDC.Community.Api.Controllers.Community.ServersController).Assembly);
            services.Configure<Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions>(options =>
            {
                var original=options.SchemaGeneratorOptions.SchemaIdSelector;
                options.CustomSchemaIds(type=>type.Namespace?.StartsWith("SCDC.Identity.Api",StringComparison.Ordinal)==true
                    ? "Identity"+original(type)
                    : type.Namespace?.StartsWith("SCDC.Community.Api.Errors",StringComparison.Ordinal)==true
                        ? "Community"+original(type) : original(type));
                options.OperationFilter<CommunityApi::SCDC.Community.Api.OpenApi.CommunityServerViewOperationFilter>();
                options.SchemaFilter<CommunityApi::SCDC.Community.Api.OpenApi.CommunityResponseSchemaFilter>();
            });
            // Requests drive provisioning deterministically. Recovery tests explicitly
            // start the worker; unrelated factory instances must not steal fault fixtures.
            foreach (var descriptor in services.Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)
                && (d.ImplementationType == typeof(SCDC.Modules.Community.Infrastructure.ChannelProvisionWorker)
                    || d.ImplementationType == typeof(SCDC.Modules.Identity.Infrastructure.Email.IdentityEmailWorker))).ToArray())
                services.Remove(descriptor);
            services
                .AddControllers()
                .AddApplicationPart(typeof(TestResponseController).Assembly);
        });
    }
}
