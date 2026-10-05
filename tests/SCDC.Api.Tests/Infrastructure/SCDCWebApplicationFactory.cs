using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using SCDC.Modules.Identity.Infrastructure.Email;
using SCDC.Modules.Identity.Infrastructure.Persistence;

namespace SCDC.Api.Tests.Infrastructure;

public sealed class SCDCWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var testDatabase = Environment.GetEnvironmentVariable("SCDC_TEST_DATABASE");
            if (!string.IsNullOrWhiteSpace(testDatabase))
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Database"] = testDatabase
                });
        });
        builder.ConfigureServices(services =>
        {
            var testDatabase = Environment.GetEnvironmentVariable("SCDC_TEST_DATABASE");
            if (!string.IsNullOrWhiteSpace(testDatabase))
                services.Replace(ServiceDescriptor.Singleton(
                    new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(testDatabase).Options));
            // Tests drive the processor explicitly; no hosted worker sends real email.
            foreach (var descriptor in services.Where(item => item.ServiceType == typeof(IHostedService)
                && item.ImplementationType == typeof(IdentityEmailWorker)).ToArray()) services.Remove(descriptor);
            services
                .AddControllers()
                .AddApplicationPart(typeof(TestResponseController).Assembly);
        });
    }
}
