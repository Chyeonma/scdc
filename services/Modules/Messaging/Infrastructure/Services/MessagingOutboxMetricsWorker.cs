using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class MessagingOutboxMetricsWorker(
    IServiceScopeFactory scopes,
    IOptions<MessagingOutboxOptions> options,
    TimeProvider timeProvider,
    ILogger<MessagingOutboxMetricsWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();
                var outstanding = db.OutboxEvents.AsNoTracking()
                    .Where(item => item.PublishedAt == null && item.EventType.StartsWith("Messaging."));
                var pending = outstanding.Where(item => item.AttemptCount < options.Value.MaxAttempts);
                var pendingCount = await pending.LongCountAsync(stoppingToken);
                var oldest = await pending.MinAsync(item => (DateTimeOffset?)item.OccurredAt, stoppingToken);
                var quarantined = await outstanding.LongCountAsync(
                    item => item.AttemptCount >= options.Value.MaxAttempts, stoppingToken);
                MessagingTelemetry.SetOutboxBacklog(pendingCount, quarantined,
                    oldest is null ? 0 : (timeProvider.GetUtcNow() - oldest.Value).TotalSeconds);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                MessagingTelemetry.RecordWorkerFailure("sample");
                logger.LogWarning("Messaging backlog sample failed with {ErrorKind}", exception.GetType().Name);
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
