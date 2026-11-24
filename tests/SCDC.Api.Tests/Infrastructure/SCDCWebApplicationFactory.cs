using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;

namespace SCDC.Api.Tests.Infrastructure;

public sealed class SCDCWebApplicationFactory : WebApplicationFactory<ApiProgram>
{
    private static readonly string CommunityKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    internal static readonly string CommunityKeyRingPath = Path.Combine(Path.GetTempPath(), $"scdc-test-keys-{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(TestJwtKeys.Configuration().Concat(new Dictionary<string, string?>
        {
            ["Modules:Community:Operations:ActiveKeyId"] = "test",
            ["Modules:Community:Operations:Keys:test"] = CommunityKey,
            ["Modules:Community:KeyRingPath"] = CommunityKeyRingPath
        })));
        builder.ConfigureServices(services =>
        {
            // Requests drive provisioning deterministically. Recovery tests explicitly
            // start the worker; unrelated factory instances must not steal fault fixtures.
            foreach (var descriptor in services.Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)
                && d.ImplementationType == typeof(SCDC.Modules.Community.Infrastructure.ChannelProvisionWorker)).ToArray())
                services.Remove(descriptor);
            services
                .AddControllers()
                .AddApplicationPart(typeof(TestResponseController).Assembly);
        });
    }
}
