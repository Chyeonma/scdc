using System.Text.Json;
using ChatService.Data;
using ChatService.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChatService.Infrastructure.Realtime;

public sealed class OutboxPublisher(
    IServiceScopeFactory scopeFactory,
    IRealtimeEventSender realtimeEventSender,
    TimeProvider timeProvider,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var publishedAny = await PublishBatchAsync(stoppingToken);
                if (!publishedAny)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unhandled error while publishing the outbox batch");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    private async Task<bool> PublishBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var events = await dbContext.OutboxEvents
            .FromSqlInterpolated($$"""
                SELECT *
                FROM outbox_events
                WHERE published_at IS NULL AND available_at <= {{now}}
                ORDER BY occurred_at
                LIMIT 50
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        if (events.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return false;
        }

        foreach (var outboxEvent in events)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<JsonElement>(outboxEvent.Payload);
                await realtimeEventSender.SendToChannelAsync(
                    outboxEvent.ChannelId,
                    outboxEvent.EventType,
                    payload,
                    cancellationToken);
                outboxEvent.PublishedAt = timeProvider.GetUtcNow();
                outboxEvent.LastError = null;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                outboxEvent.AttemptCount++;
                var delaySeconds = Math.Min(60, Math.Pow(2, Math.Min(outboxEvent.AttemptCount, 6)));
                outboxEvent.AvailableAt = timeProvider.GetUtcNow().AddSeconds(delaySeconds);
                outboxEvent.LastError = exception.Message.Length <= 2000
                    ? exception.Message
                    : exception.Message[..2000];
                logger.LogWarning(
                    exception,
                    "Could not publish outbox event {EventId} of type {EventType}",
                    outboxEvent.Id,
                    outboxEvent.EventType);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
