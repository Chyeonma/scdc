using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;

namespace SCDC.Modules.Community.Features.Servers.Application;

public record ServerSummary(Guid Id, string Name, string? Description, string Visibility, string JoinMode, string Version);
public sealed record ServerDetail(Guid Id, string Name, string? Description, string Visibility, string JoinMode, string Version,
    Guid OwnerUserId, string AccessVersion, MembershipView MyMembership, IReadOnlyList<string> EffectivePermissions)
    : ServerSummary(Id, Name, Description, Visibility, JoinMode, Version);
public sealed record MembershipView(Guid ServerId, Guid UserId, Guid MembershipId, string Status,
    DateTimeOffset JoinedAt, DateTimeOffset? LeftAt, string Version);
public sealed record MyServerPage(IReadOnlyList<ServerDetail> Items, string? NextCursor);
public sealed record CreateServerCommand(Guid ClientOperationId, string? Name, string? Description, string? Visibility);
public sealed record CreateServerResult(ServerDetail Server, bool Created);

public interface IServerService
{
    Task<Result<CreateServerResult>> CreateAsync(AccountActor actor, CreateServerCommand command, CancellationToken cancellationToken);
    Task<Result<MyServerPage>> ListAsync(AccountActor actor, int limit, string? cursor, CancellationToken cancellationToken);
    Task<Result<ServerSummary>> GetAsync(AccountActor actor, Guid serverId, CancellationToken cancellationToken);
    Task<Result<MembershipView>> GetMembershipAsync(AccountActor actor, Guid serverId, CancellationToken cancellationToken);
}
