using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;
using SCDC.Modules.Community.Features.Servers.Application;

namespace SCDC.Modules.Community.Features.Permissions.Application;

public sealed record RoleView(Guid Id, Guid ServerId, string Name, bool IsSystem, IReadOnlyList<string> Permissions, string Version);
public sealed record RolePage(IReadOnlyList<RoleView> Items, string? NextCursor);
public sealed record MemberView(MembershipView Membership, UserSummary? User);
public sealed record MemberPage(IReadOnlyList<MemberView> Items, string? NextCursor);
public sealed record MemberRoles(Guid ServerId, Guid UserId, Guid MembershipId, string Version, IReadOnlyList<Guid> RoleIds);
public sealed record CreateRoleCommand(Guid ClientOperationId, string? Name, IReadOnlyList<string>? Permissions);
public sealed record UpdateRoleCommand(string? ExpectedVersion, bool HasName, string? Name, bool HasPermissions, IReadOnlyList<string>? Permissions);
public sealed record ReplaceMemberRolesCommand(Guid MembershipId, string? ExpectedVersion, IReadOnlyList<Guid>? RoleIds);
public sealed record CreateRoleResult(RoleView Role, bool Created);

public interface IRoleService
{
    Task<Result<RolePage>> ListAsync(AccountActor actor, Guid server, int limit, string? cursor, CancellationToken ct);
    Task<Result<CreateRoleResult>> CreateAsync(AccountActor actor, Guid server, CreateRoleCommand command, CancellationToken ct);
    Task<Result<RoleView>> UpdateAsync(AccountActor actor, Guid server, Guid role, UpdateRoleCommand command, CancellationToken ct);
    Task<Result> DeleteAsync(AccountActor actor, Guid server, Guid role, string? expectedVersion, CancellationToken ct);
    Task<Result<MemberPage>> MembersAsync(AccountActor actor, Guid server, int limit, string? cursor, CancellationToken ct);
    Task<Result<MemberRoles>> MemberRolesAsync(AccountActor actor, Guid server, Guid user, CancellationToken ct);
    Task<Result<MemberRoles>> ReplaceAsync(AccountActor actor, Guid server, Guid user, ReplaceMemberRolesCommand command, CancellationToken ct);
}
internal sealed class PermissionFailure(Error error) : Exception
{
    public Error Error { get; } = error;
    public static PermissionFailure Missing() => new(Error.NotFound("RESOURCE_NOT_FOUND", "Resource was not found."));
    public static PermissionFailure Conflict(string code, string message) => new(Error.Conflict(code, message));
    public static PermissionFailure Invalid(string field, string message) => new(new ValidationError("VALIDATION_FAILED", "Invalid role data.", new Dictionary<string,string[]> {{field,[message]}}));
}
