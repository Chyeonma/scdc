using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SCDC.Modules.Messaging.Infrastructure;

/// <summary>Low-cardinality process metrics. Resource IDs belong in structured logs, never metric tags.</summary>
public static class MessagingTelemetry
{
    public const string MeterName = "SCDC.Messaging";
    public const string ActivitySourceName = "SCDC.Messaging";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    private static readonly Meter Meter = new(MeterName);
    private static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
        "scdc.messaging.request.duration", "s");
    private static readonly Counter<long> SendFailures = Meter.CreateCounter<long>(
        "scdc.messaging.send.failures");
    private static readonly Counter<long> DuplicateRetries = Meter.CreateCounter<long>(
        "scdc.messaging.send.duplicate_retries");
    private static readonly UpDownCounter<long> ActiveConnections = Meter.CreateUpDownCounter<long>(
        "scdc.messaging.connections.active");
    private static readonly Counter<long> Reconnections = Meter.CreateCounter<long>(
        "scdc.messaging.connections.reconnected");
    private static readonly Histogram<double> DispatchDuration = Meter.CreateHistogram<double>(
        "scdc.messaging.outbox.dispatch.duration", "s");
    private static readonly Histogram<double> DeliveryLag = Meter.CreateHistogram<double>(
        "scdc.messaging.outbox.delivery.lag", "s");
    private static readonly Counter<long> DispatchFailures = Meter.CreateCounter<long>(
        "scdc.messaging.outbox.dispatch.failures");
    private static readonly Counter<long> WorkerFailures = Meter.CreateCounter<long>(
        "scdc.messaging.outbox.worker.failures");
    private static readonly Counter<long> QuarantinedEvents = Meter.CreateCounter<long>(
        "scdc.messaging.outbox.quarantined.total");
    private static readonly Counter<long> StorageFailures = Meter.CreateCounter<long>(
        "scdc.messaging.storage.failures");
    private static long _pending;
    private static long _quarantined;
    private static double _oldestAgeSeconds;

    static MessagingTelemetry()
    {
        Meter.CreateObservableGauge("scdc.messaging.outbox.pending", () => Interlocked.Read(ref _pending));
        Meter.CreateObservableGauge("scdc.messaging.outbox.quarantined", () => Interlocked.Read(ref _quarantined));
        Meter.CreateObservableGauge("scdc.messaging.outbox.oldest.age", () => Volatile.Read(ref _oldestAgeSeconds), "s");
    }

    public static void RecordRequest(string operation, int statusCode, double elapsedSeconds)
    {
        var outcome = statusCode < 400 ? "ok" : statusCode < 500 ? "rejected" : "error";
        RequestDuration.Record(elapsedSeconds, new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("outcome", outcome));
        if (operation == "send" && statusCode >= 400)
            SendFailures.Add(1, new KeyValuePair<string, object?>("outcome", outcome));
    }

    public static void RecordDuplicateRetry() => DuplicateRetries.Add(1);

    public static void ConnectionOpened(bool reconnected)
    {
        ActiveConnections.Add(1);
        if (reconnected) Reconnections.Add(1);
    }

    public static void ConnectionClosed() => ActiveConnections.Add(-1);

    public static void RecordDispatch(double elapsedSeconds, bool succeeded)
    {
        DispatchDuration.Record(elapsedSeconds,
            new KeyValuePair<string, object?>("outcome", succeeded ? "ok" : "error"));
        if (!succeeded) DispatchFailures.Add(1);
    }

    public static void RecordDeliveryLag(double seconds) => DeliveryLag.Record(Math.Max(0, seconds));

    public static void RecordQuarantine() => QuarantinedEvents.Add(1);

    public static void RecordWorkerFailure(string operation) =>
        WorkerFailures.Add(1, new KeyValuePair<string, object?>("operation", operation));

    public static void RecordStorageFailure(string operation) =>
        StorageFailures.Add(1, new KeyValuePair<string, object?>("operation", operation));

    public static void SetOutboxBacklog(long pending, long quarantined, double oldestAgeSeconds)
    {
        Interlocked.Exchange(ref _pending, pending);
        Interlocked.Exchange(ref _quarantined, quarantined);
        Volatile.Write(ref _oldestAgeSeconds, Math.Max(0, oldestAgeSeconds));
    }
}
