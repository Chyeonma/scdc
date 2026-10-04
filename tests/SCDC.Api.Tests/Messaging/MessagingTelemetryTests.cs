using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using SCDC.Modules.Messaging.Hubs;
using SCDC.Modules.Messaging.Infrastructure;

namespace SCDC.Api.Tests.Messaging;

public sealed class MessagingTelemetryTests
{
    [Fact]
    public void Request_connection_and_outbox_instruments_emit_bounded_metrics()
    {
        var observed = new ConcurrentBag<(string Name, double Value, string Tags)>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == MessagingTelemetry.MeterName)
                    meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
            observed.Add((instrument.Name, value, string.Join(",", tags.ToArray().Select(tag => tag.Key + "=" + tag.Value)))));
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            observed.Add((instrument.Name, value, string.Join(",", tags.ToArray().Select(tag => tag.Key + "=" + tag.Value)))));
        listener.Start();

        MessagingTelemetry.RecordRequest("send", 429, 0.05);
        MessagingTelemetry.RecordDuplicateRetry();
        MessagingTelemetry.RecordDispatch(0.1, succeeded: false);
        MessagingTelemetry.RecordDeliveryLag(1.2);
        MessagingTelemetry.RecordQuarantine();
        var connections = new RealtimeConnectionRegistry();
        var user = Guid.NewGuid();
        var session = Guid.NewGuid();
        connections.Register(user, session, "first", () => { });
        connections.Remove("first");
        connections.Register(user, session, "second", () => { });
        connections.Remove("second");

        Assert.Contains(observed, item => item.Name == "scdc.messaging.request.duration"
            && item.Tags == "operation=send,outcome=rejected");
        Assert.Contains(observed, item => item.Name == "scdc.messaging.send.failures");
        Assert.Contains(observed, item => item.Name == "scdc.messaging.send.duplicate_retries");
        Assert.Contains(observed, item => item.Name == "scdc.messaging.outbox.dispatch.duration");
        Assert.Contains(observed, item => item.Name == "scdc.messaging.outbox.delivery.lag");
        Assert.Contains(observed, item => item.Name == "scdc.messaging.outbox.quarantined.total");
        Assert.Contains(observed, item => item.Name == "scdc.messaging.connections.reconnected");
        Assert.DoesNotContain(observed, item => item.Tags.Contains(user.ToString("D")));
    }
}
