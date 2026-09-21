using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SCDC.Modules.Messaging.Application;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class MessagingOutboxWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<MessagingOutboxOptions> options,
    ILogger<MessagingOutboxWorker> logger) : BackgroundService
{
    private readonly MessagingOutboxOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<IMessagingOutboxDispatcher>();
                var processed = await dispatcher.DispatchDueAsync(stoppingToken);
                if (processed == 0)
                {
                    await Task.Delay(_options.PollInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Messaging outbox worker iteration failed.");
                await Task.Delay(_options.PollInterval, stoppingToken);
            }
        }
    }
}
