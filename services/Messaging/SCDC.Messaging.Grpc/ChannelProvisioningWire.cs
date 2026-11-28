using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using SCDC.Contracts.Messaging;
using M = SCDC.Messaging.Grpc.V1;

namespace SCDC.Messaging.Grpc;

public static class ChannelProvisioningWire
{
    public static M.PrepareChannel Write(PrepareChannel value) => new()
    {
        OperationId = value.OperationId.ToString("D"), ChannelId = value.ChannelId.ToString("D"), ServerId = value.ServerId.ToString("D"),
        ActorId = value.ActorId.ToString("D"), CreatedAt = Timestamp.FromDateTimeOffset(value.CreatedAt),
        Fingerprint = ByteString.CopyFrom(value.Fingerprint), RoutingGeneration = checked((ulong)value.RoutingGeneration), ChannelEpoch = checked((ulong)value.ChannelEpoch)
    };
    public static PrepareChannel Read(M.PrepareChannel value) => new(Guid.Parse(value.OperationId), Guid.Parse(value.ChannelId),
        Guid.Parse(value.ServerId), Guid.Parse(value.ActorId), (value.CreatedAt ?? throw new FormatException()).ToDateTimeOffset(),
        value.Fingerprint.ToByteArray(), checked((long)value.RoutingGeneration), checked((long)value.ChannelEpoch));
    public static M.ChannelDecision Write(ChannelDecision value) => new()
    {
        OperationId = value.OperationId.ToString("D"), ChannelId = value.ChannelId.ToString("D"), Fingerprint = ByteString.CopyFrom(value.Fingerprint),
        DecisionVersion = checked((ulong)value.DecisionVersion), RoutingGeneration = checked((ulong)value.RoutingGeneration), ChannelEpoch = checked((ulong)value.ChannelEpoch)
    };
    public static ChannelDecision Read(M.ChannelDecision value) => new(Guid.Parse(value.OperationId), Guid.Parse(value.ChannelId),
        value.Fingerprint.ToByteArray(), checked((long)value.DecisionVersion), checked((long)value.RoutingGeneration), checked((long)value.ChannelEpoch));
    public static M.ProvisionStatus Write(ProvisionStatus? value) => value is null ? new() { Missing = true } : new()
    {
        OperationId = value.OperationId.ToString("D"), ChannelId = value.ChannelId.ToString("D"),
        Phase = (M.ProvisionPhase)((int)value.Phase + 1), DecisionVersion = checked((ulong)value.DecisionVersion), CleanupPending = value.CleanupPending
    };
    public static ProvisionStatus? Read(M.ProvisionStatus value)
    {
        if (value.Missing)
        {
            if (value.OperationId.Length != 0 || value.ChannelId.Length != 0 || (int)value.Phase != 0 || value.DecisionVersion != 0 || value.CleanupPending) throw new FormatException();
            return null;
        }
        if ((int)value.Phase is < 1 or > 3) throw new FormatException();
        if (value.DecisionVersion != ((int)value.Phase == 1 ? 0UL : 1UL)) throw new FormatException();
        return new(Guid.Parse(value.OperationId), Guid.Parse(value.ChannelId), (ProvisionPhase)((int)value.Phase - 1), checked((long)value.DecisionVersion), value.CleanupPending);
    }
}
