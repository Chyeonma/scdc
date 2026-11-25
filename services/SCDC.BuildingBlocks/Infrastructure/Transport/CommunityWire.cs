using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using SCDC.Contracts.Community;
using SCDC.Contracts.Identity;
using SCDC.Contracts.Messaging;
using C = SCDC.Community.Grpc.V1;
using M = SCDC.Messaging.Grpc.V1;

namespace SCDC.BuildingBlocks.Infrastructure.Transport;

public static class CommunityWire
{
    public static C.CommunityScope Write(CommunityScope scope) => new()
    {
        Kind = checked((uint)scope.Kind), UserId = scope.UserId.ToString("D"), ServerId = scope.ServerId.ToString("D"),
        ChannelId = scope.ChannelId.ToString("D"), MembershipId = scope.MembershipId.ToString("D"),
        Permission = scope.Permission, Epoch = checked((ulong)scope.Epoch)
    };
    public static CommunityScope Read(C.CommunityScope scope) => new(checked((short)scope.Kind),
        Guid.Parse(scope.UserId), Guid.Parse(scope.ServerId), Guid.Parse(scope.ChannelId), Guid.Parse(scope.MembershipId),
        scope.Permission, checked((long)scope.Epoch));
    public static C.CommunityFenceBatch Write(CommunityFenceBatch batch)
    {
        var result = new C.CommunityFenceBatch { OperationId = batch.OperationId.ToString("D"), BatchId = batch.BatchId.ToString("D"), RoutingGeneration = checked((ulong)batch.RoutingGeneration) };
        result.Scopes.AddRange(batch.Scopes.Select(Write)); return result;
    }
    public static CommunityFenceBatch Read(C.CommunityFenceBatch batch) => new(Guid.Parse(batch.OperationId), Guid.Parse(batch.BatchId),
        checked((long)batch.RoutingGeneration), batch.Scopes.Select(Read).ToArray());
    public static C.CommunityFenceReceipt Write(CommunityFenceReceipt receipt) => new()
    { Owner = (SCDC.Identity.Grpc.V1.Owner)(int)receipt.Owner, Batch = Write(receipt.Batch) };
    public static CommunityFenceReceipt Read(C.CommunityFenceReceipt receipt) => new((AccessOwner)(int)receipt.Owner,
        Read(receipt.Batch ?? throw new FormatException()));
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
    public static C.ChannelReadyPermit Write(ChannelReadyPermit value)
    {
        var result = new C.ChannelReadyPermit { OperationId = value.OperationId.ToString("D"), ChannelId = value.ChannelId.ToString("D"), ServerId = value.ServerId.ToString("D"),
            Fingerprint = ByteString.CopyFrom(value.Fingerprint), Identity = IdentityWire.Write(value.Identity),
            IssuedAt = Timestamp.FromDateTimeOffset(value.IssuedAt), ExpiresAt = Timestamp.FromDateTimeOffset(value.ExpiresAt) };
        result.Scopes.AddRange(value.Scopes.Select(Write)); return result;
    }
    public static ChannelReadyPermit Read(C.ChannelReadyPermit value) => new(Guid.Parse(value.OperationId), Guid.Parse(value.ChannelId), Guid.Parse(value.ServerId),
        value.Fingerprint.ToByteArray(), IdentityWire.Read(value.Identity ?? throw new FormatException()), value.Scopes.Select(Read).ToArray(),
        (value.IssuedAt ?? throw new FormatException()).ToDateTimeOffset(), (value.ExpiresAt ?? throw new FormatException()).ToDateTimeOffset());
}
