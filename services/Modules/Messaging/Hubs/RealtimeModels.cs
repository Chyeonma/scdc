namespace SCDC.Modules.Messaging.Hubs;

public sealed record HubResult<T>(bool Ok, T? Value, HubError? Error)
{
    public static HubResult<T> Success(T value) => new(true, value, null);
    public static HubResult<T> Failure(string errorCode, string message, string traceId) =>
        new(false, default, new HubError(errorCode, message, traceId));
}

public sealed record HubError(string ErrorCode, string Message, string TraceId);
public sealed record SubscribeSpaceResponse(Guid SpaceId, string HighWatermark);
public sealed record UnsubscribeSpaceResponse(Guid SpaceId);

public sealed record RealtimeEventEnvelope(
    Guid EventId,
    string EventType,
    int SchemaVersion,
    Guid? SpaceId,
    DateTimeOffset OccurredAt,
    int? AggregateVersion,
    object Payload);

internal sealed record MessageCreatedPayload(Guid MessageId, string SequenceNo);
