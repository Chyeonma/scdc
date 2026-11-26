extern alias IdentityApi;
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
            // Legacy component regressions compose Identity in TestServer only.
            // Independent-host tests exercise the production gRPC path separately.
            SCDC.Modules.Identity.IdentityModule.AddIdentityModule(services, context.Configuration, configureAuthentication: false);
            services.AddControllers().AddApplicationPart(typeof(IdentityApi::SCDC.Identity.Api.Controllers.Identity.AuthController).Assembly);
            services.Configure<Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions>(options =>
            {
                var original=options.SchemaGeneratorOptions.SchemaIdSelector;
                options.CustomSchemaIds(type=>type.Namespace?.StartsWith("SCDC.Identity.Api",StringComparison.Ordinal)==true
                    ? "Identity"+original(type) : original(type));
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
