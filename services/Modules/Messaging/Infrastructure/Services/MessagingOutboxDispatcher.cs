using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Domain;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class MessagingOutboxDispatcher(
    MessagingDbContext dbContext,
    IRealtimeMessagePublisher realtimePublisher,
    IOptions<MessagingOutboxOptions> options,
    TimeProvider timeProvider,
    ILogger<MessagingOutboxDispatcher> logger) : IMessagingOutboxDispatcher
{
    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly MessagingOutboxOptions _options = options.Value;

    public async Task<int> DispatchDueAsync(CancellationToken cancellationToken)
    {
        var processed = 0;
        while (processed < _options.BatchSize
               && await DispatchOneAsync(cancellationToken))
        {
            processed++;
        }

        return processed;
    }

    public async Task<IReadOnlyList<MessagingOutboxFailure>> ListFailuresAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(limit, 1, 100);
        return await dbContext.OutboxEvents
            .AsNoTracking()
            .Where(item => item.EventType.StartsWith("Messaging.")
                           && item.PublishedAt == null
                           && item.AttemptCount >= _options.MaxAttempts)
            .OrderBy(item => item.AvailableAt)
            .ThenBy(item => item.OccurredAt)
            .Take(take)
            .Select(item => new MessagingOutboxFailure(
                item.Id,
                item.EventType,
                item.AggregateId,
                item.SpaceId,
                item.AttemptCount,
                item.AvailableAt,
                item.OccurredAt,
                item.LastError))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ReplayAsync(Guid eventId, CancellationToken cancellationToken)
    {
        if (eventId == Guid.Empty)
        {
            return false;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var item = await dbContext.OutboxEvents
            .FromSqlInterpolated($"""
                SELECT *
                FROM integration.outbox_events
                WHERE id = {eventId}
                  AND published_at IS NULL
                  AND event_type LIKE 'Messaging.%'
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);
        if (item is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return false;
        }

        item.AttemptCount = 0;
        item.LastError = null;
        item.AvailableAt = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<bool> DispatchOneAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var item = await dbContext.OutboxEvents
            .FromSqlInterpolated($"""
                SELECT *
                FROM integration.outbox_events
                WHERE published_at IS NULL
                  AND available_at <= {now}
                  AND attempt_count < {_options.MaxAttempts}
                  AND event_type LIKE 'Messaging.%'
                ORDER BY available_at, occurred_at, id
                FOR UPDATE SKIP LOCKED
                LIMIT 1
                """)
            .SingleOrDefaultAsync(cancellationToken);
        if (item is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return false;
        }

        try
        {
            await DispatchAsync(item, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            item.AttemptCount++;
            item.LastError = null;
            item.PublishedAt = timeProvider.GetUtcNow();
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            item.AttemptCount++;
            item.LastError = ToSafeError(exception);
            item.AvailableAt = now.Add(CalculateRetryDelay(item.AttemptCount));
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogWarning(
                exception,
                "Messaging outbox event {EventId} failed on attempt {AttemptCount}; retry scheduled for {AvailableAt}",
                item.Id,
                item.AttemptCount,
                item.AvailableAt);
        }

        return true;
    }

    private async Task DispatchAsync(OutboxEvent item, CancellationToken cancellationToken)
    {
        switch (item.EventType)
        {
            case "Messaging.MessageCreated":
                var payload = JsonSerializer.Deserialize<MessageCreatedOutboxPayload>(item.Payload, PayloadJsonOptions)
                    ?? throw new InvalidOperationException("MessageCreated outbox payload is missing.");
                if (item.SpaceId is not { } spaceId
                    || item.AggregateVersion is not { } aggregateVersion)
                {
                    throw new InvalidOperationException("MessageCreated outbox payload is invalid.");
                }

                if (aggregateVersion < 1
                    || payload.MessageId == Guid.Empty
                    || string.IsNullOrWhiteSpace(payload.SequenceNo))
                {
                    throw new InvalidOperationException("MessageCreated outbox payload is invalid.");
                }

                await realtimePublisher.PublishMessageCreatedAsync(
                    new RealtimeMessageCreated(
                        item.Id,
                        spaceId,
                        payload.MessageId,
                        payload.SequenceNo,
                        aggregateVersion,
                        item.OccurredAt),
                    cancellationToken);
                return;
            default:
                throw new InvalidOperationException($"Unsupported Messaging outbox event type '{item.EventType}'.");
        }
    }

    private TimeSpan CalculateRetryDelay(int attemptCount)
    {
        var exponent = Math.Clamp(attemptCount - 1, 0, 20);
        var multiplier = 1L << exponent;
        var scaledTicks = _options.InitialRetryDelay.Ticks > long.MaxValue / multiplier
            ? long.MaxValue
            : _options.InitialRetryDelay.Ticks * multiplier;
        return TimeSpan.FromTicks(Math.Min(scaledTicks, _options.MaxRetryDelay.Ticks));
    }

    private static string ToSafeError(Exception exception)
    {
        var message = exception.GetBaseException().Message;
        return message.Length <= 1000 ? message : message[..1000];
    }

    private sealed record MessageCreatedOutboxPayload(Guid MessageId, string SequenceNo);
}
