using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using SCDC.Modules.Messaging.Application;

namespace SCDC.Api.Tests.Infrastructure;

public class SCDCWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly bool _useRealtimePublisher;
    private readonly ILoggerProvider? _additionalLogger;
    private string? _contentRootOverride;

    public SCDCWebApplicationFactory() : this(false) { }

    internal SCDCWebApplicationFactory(bool useRealtimePublisher, ILoggerProvider? additionalLogger = null)
    {
        _useRealtimePublisher = useRealtimePublisher;
        _additionalLogger = additionalLogger;
    }

    public static SCDCWebApplicationFactory ForPerformance(string contentRoot) =>
        new(useRealtimePublisher: true) { _contentRootOverride = contentRoot };

    public TestOutboxPublisher OutboxPublisher { get; } = new();
    public TestAttachmentObjectStore AttachmentStore { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (_contentRootOverride is not null) builder.UseContentRoot(_contentRootOverride);
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            if (_additionalLogger is not null) logging.AddProvider(_additionalLogger);
        });
        builder.ConfigureServices(services =>
        {
            services
                .AddControllers()
                .AddApplicationPart(typeof(TestResponseController).Assembly);
            if (!_useRealtimePublisher)
            {
                services.AddSingleton(OutboxPublisher);
                services.AddScoped<IRealtimeMessagePublisher>(provider =>
                    provider.GetRequiredService<TestOutboxPublisher>());
            }
            services.RemoveAll<IAttachmentObjectStore>();
            services.RemoveAll<IFileScanner>();
            services.AddSingleton<IAttachmentObjectStore>(AttachmentStore);
            services.AddSingleton<IFileScanner, TestFileScanner>();
        });
    }
}
