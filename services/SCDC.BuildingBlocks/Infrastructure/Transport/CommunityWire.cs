using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using SCDC.Contracts.Community;
using SCDC.Contracts.Identity;
using C = SCDC.Community.Grpc.V1;

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
