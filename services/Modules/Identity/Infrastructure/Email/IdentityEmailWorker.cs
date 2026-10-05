using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SCDC.Modules.Identity.Infrastructure.Email;

internal sealed class IdentityEmailWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<IdentityEmailOptions> options,
    TimeProvider timeProvider,
    ILogger<IdentityEmailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var nextMaintenance = DateTimeOffset.MinValue;
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = options.Value.PollSeconds;
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IdentityEmailProcessor>().ProcessBatchAsync(stoppingToken);
                if (timeProvider.GetUtcNow() >= nextMaintenance)
                {
                    await scope.ServiceProvider.GetRequiredService<IdentityMaintenance>().RunAsync(stoppingToken);
                    nextMaintenance = timeProvider.GetUtcNow().AddHours(1);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // Exception messages/SMTP responses can contain addresses or credentials.
                logger.LogWarning("Identity email cycle failed ({ErrorType}); retrying in 30 seconds.", exception.GetType().Name);
                delay = 30;
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delay), timeProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
