using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;

namespace SCDC.Api.Tests.Infrastructure;

public sealed class SCDCWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly string CommunityKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    internal static readonly string CommunityKeyRingPath = Path.Combine(Path.GetTempPath(), $"scdc-test-keys-{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Modules:Community:Operations:ActiveKeyId"] = "test",
            ["Modules:Community:Operations:Keys:test"] = CommunityKey,
            ["Modules:Community:KeyRingPath"] = CommunityKeyRingPath
        }));
        builder.ConfigureServices(services =>
        {
            services
                .AddControllers()
                .AddApplicationPart(typeof(TestResponseController).Assembly);
        });
    }
}
