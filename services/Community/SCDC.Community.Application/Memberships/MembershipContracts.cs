using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;
using SCDC.Modules.Community.Features.Servers.Application;

namespace SCDC.Modules.Community.Features.Memberships.Application;

public interface IMembershipService
{
    Task<Result<MembershipView>> JoinAsync(AccountActor actor, Guid serverId, CancellationToken cancellationToken);
}
