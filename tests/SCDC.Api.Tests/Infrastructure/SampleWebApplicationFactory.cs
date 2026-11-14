using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SCDC.Api.Tests.Infrastructure;

public sealed class SampleWebApplicationFactory : WebApplicationFactory<SampleProgram>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            TestJwtKeys.Configuration().Where(kv => kv.Key.StartsWith("Authentication:", StringComparison.Ordinal))));
    }
}
