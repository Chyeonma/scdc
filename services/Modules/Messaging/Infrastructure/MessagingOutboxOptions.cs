using System.ComponentModel.DataAnnotations;

namespace SCDC.Modules.Messaging.Infrastructure;

internal sealed class MessagingOutboxOptions
{
    public const string SectionName = "Modules:Messaging:Outbox";

    public bool Enabled { get; init; } = true;

    [Range(1, 100)]
    public int BatchSize { get; init; } = 20;

    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(1);

    public TimeSpan InitialRetryDelay { get; init; } = TimeSpan.FromSeconds(1);

    public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromMinutes(5);

    [Range(1, 100)]
    public int MaxAttempts { get; init; } = 10;
}
