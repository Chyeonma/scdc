using Google.Protobuf.WellKnownTypes;
using SCDC.Contracts.Identity;
using W = SCDC.Identity.Grpc.V1;

namespace SCDC.BuildingBlocks.Infrastructure.Transport;

public static class IdentityWire
{
    public static W.AccessRequest Write(PermitRequest request, AccessOwner owner) => new()
    {
        Actor = new W.Actor { UserId = request.Actor.UserId.ToString("D"), SessionId = request.Actor.SessionId.ToString("D"), SecurityStamp = request.Actor.SecurityStamp.ToString("D") },
        Owner = (W.Owner)(int)owner, Purpose = (W.Purpose)(int)request.Purpose,
        ResourceId = request.ResourceId.ToString("D"), OperationId = request.OperationId.ToString("D"),
        PeerId = request.PeerId?.ToString("D") ?? "", RoutingGeneration = checked((ulong)request.RoutingGeneration),
        Context = (W.AccessContext)(int)request.Context, AccessExpiresAt = Timestamp.FromDateTimeOffset(request.Actor.AccessExpiresAt)
    };
    public static PermitRequest Read(W.AccessRequest request)
    {
        if (request.Actor is null || request.AccessExpiresAt is null) throw new FormatException();
        return new(new(Guid.Parse(request.Actor.UserId), Guid.Parse(request.Actor.SessionId), Guid.Parse(request.Actor.SecurityStamp), request.AccessExpiresAt.ToDateTimeOffset()),
            (IdentityPurpose)(int)request.Purpose, Guid.Parse(request.ResourceId), Guid.Parse(request.OperationId),
            request.PeerId.Length == 0 ? null : Guid.Parse(request.PeerId), checked((long)request.RoutingGeneration), (IdentityAccessContext)(int)request.Context);
    }
    public static W.ScopeStamp Write(IdentityScope scope) => new()
    {
        Scope = new W.ScopeRef { Authority = W.Authority.Identity, Kind = (W.ScopeKind)scope.Kind,
            UserId = scope.UserId.ToString("D"), SessionId = scope.SessionId.ToString("D") }, Epoch = checked((ulong)scope.Epoch)
    };
    public static IdentityScope Read(W.ScopeStamp stamp)
    {
        var scope = stamp.Scope ?? throw new FormatException();
        if (scope.Authority != W.Authority.Identity || scope.Kind is not (W.ScopeKind.Account or W.ScopeKind.Session)
            || scope.ServerId.Length != 0 || scope.ChannelId.Length != 0 || scope.MembershipId.Length != 0 || scope.PermissionCode.Length != 0)
            throw new FormatException();
        return new((short)scope.Kind, Guid.Parse(scope.UserId), Guid.Parse(scope.SessionId), checked((long)stamp.Epoch));
    }
    public static W.Permit Write(IdentityPermit permit)
    {
        var request = Write(permit.Request, permit.Owner);
        var wire = new W.Permit { PermitId = permit.Id.ToString("D"), Issuer = W.Authority.Identity,
            Owner = request.Owner, Actor = request.Actor, Purpose = request.Purpose, ResourceId = request.ResourceId,
            PeerId = request.PeerId, OperationId = request.OperationId, RoutingGeneration = request.RoutingGeneration,
            IssuedAt = Timestamp.FromDateTimeOffset(permit.IssuedAt), ExpiresAt = Timestamp.FromDateTimeOffset(permit.ExpiresAt), Request = request };
        wire.Scopes.AddRange(permit.Scopes.Select(Write)); return wire;
    }
    public static IdentityPermit Read(W.Permit wire)
    {
        if (wire.Issuer != W.Authority.Identity || wire.Request is null || wire.IssuedAt is null || wire.ExpiresAt is null
            || wire.Owner != wire.Request.Owner || !Equals(wire.Actor, wire.Request.Actor) || wire.Purpose != wire.Request.Purpose
            || wire.ResourceId != wire.Request.ResourceId || wire.PeerId != wire.Request.PeerId
            || wire.OperationId != wire.Request.OperationId || wire.RoutingGeneration != wire.Request.RoutingGeneration) throw new FormatException();
        return new(Guid.Parse(wire.PermitId), (AccessOwner)(int)wire.Owner, Read(wire.Request), wire.Scopes.Select(Read).ToArray(), wire.IssuedAt.ToDateTimeOffset(), wire.ExpiresAt.ToDateTimeOffset());
    }
    public static W.FenceBatch Write(IdentityFenceBatch batch)
    {
        var wire = new W.FenceBatch { Issuer = W.Authority.Identity, OperationId = batch.OperationId.ToString("D"), BatchId = batch.BatchId.ToString("D"), RoutingGeneration = checked((ulong)batch.RoutingGeneration) };
        wire.Floors.AddRange(batch.Scopes.Select(Write)); return wire;
    }
    public static IdentityFenceBatch Read(W.FenceBatch batch)
    {
        if (batch.Issuer != W.Authority.Identity) throw new FormatException();
        return new(Guid.Parse(batch.OperationId), Guid.Parse(batch.BatchId), checked((long)batch.RoutingGeneration), batch.Floors.Select(Read).ToArray());
    }
    public static W.FenceReceipt Write(IdentityFenceReceipt receipt)
    {
        var wire = new W.FenceReceipt { Owner = (W.Owner)(int)receipt.Owner, OperationId = receipt.Batch.OperationId.ToString("D"), BatchId = receipt.Batch.BatchId.ToString("D"), RoutingGeneration = checked((ulong)receipt.Batch.RoutingGeneration) };
        wire.InstalledFloors.AddRange(receipt.Batch.Scopes.Select(Write)); return wire;
    }
    public static IdentityFenceReceipt Read(W.FenceReceipt receipt) => new((AccessOwner)(int)receipt.Owner,
        new(Guid.Parse(receipt.OperationId), Guid.Parse(receipt.BatchId), checked((long)receipt.RoutingGeneration), receipt.InstalledFloors.Select(Read).ToArray()));
}
