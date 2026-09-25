using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using SCDC.Modules.Messaging.Application;

namespace SCDC.Api.Tests.Infrastructure;

public class SCDCWebApplicationFactory : WebApplicationFactory<Program>
{
    public TestOutboxPublisher OutboxPublisher { get; } = new();
    public TestAttachmentObjectStore AttachmentStore { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            services
                .AddControllers()
                .AddApplicationPart(typeof(TestResponseController).Assembly);
            services.AddSingleton(OutboxPublisher);
            services.AddScoped<IRealtimeMessagePublisher>(provider =>
                provider.GetRequiredService<TestOutboxPublisher>());
            services.RemoveAll<IAttachmentObjectStore>();
            services.RemoveAll<IFileScanner>();
            services.AddSingleton<IAttachmentObjectStore>(AttachmentStore);
            services.AddSingleton<IFileScanner, TestFileScanner>();
        });
    }
}
