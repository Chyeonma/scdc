using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;
using SCDC.Modules.Messaging.Domain;

namespace SCDC.Modules.Messaging.Application.GroupConversations;

public sealed record CreateGroup(AuthenticatedSession Actor, Guid Id, string? Name, IReadOnlyList<Guid>? MemberIds);
public sealed record ListGroups(AuthenticatedSession Actor, int Limit, Guid? After);
public sealed record GroupSummary(Guid Id, string Name, Guid OwnerUserId, int MemberCount,
    DateTimeOffset CreatedAt, string LastSequence, DateTimeOffset? LastActivityAt);
public sealed record GroupPage(IReadOnlyList<GroupSummary> Items, Guid? NextCursor);

internal interface IGroupConversationStore
{
    Task<Result<GroupSummary>> CreateAsync(AuthenticatedSession actor, Guid id, string name, Guid[] memberIds, CancellationToken token);
    Task<Result<GroupPage>> ListAsync(ListGroups query, CancellationToken token);
}

public sealed class CreateGroupHandler
{
    private readonly IGroupConversationStore store;
    internal CreateGroupHandler(IGroupConversationStore store) => this.store = store;

    public Task<Result<GroupSummary>> HandleAsync(CreateGroup command, CancellationToken token)
    {
        var members = command.MemberIds;
        var name = command.Name?.Trim();
        if (command.Id.Version != 4 || name is null || name.Length is < 1 or > 100 || name.Any(char.IsControl)
            || TextContent.Validate(name).Error is not null
            || members is null || members.Count < 2 || members.Count >= GroupConversation.MaxMembers
            || members.Any(id => id == Guid.Empty || id == command.Actor.UserId) || members.Distinct().Count() != members.Count)
        {
            return Task.FromResult(Result.Failure<GroupSummary>(Error.Validation("Common.ValidationFailed",
                "Provide a UUID v4, a group name of 1–100 characters, and 2–9 distinct people other than yourself.")));
        }

        return store.CreateAsync(command.Actor, command.Id, name, members.ToArray(), token);
    }
}

public sealed class ListGroupsHandler
{
    private readonly IGroupConversationStore store;
    internal ListGroupsHandler(IGroupConversationStore store) => this.store = store;

    public Task<Result<GroupPage>> HandleAsync(ListGroups query, CancellationToken token) =>
        query.Limit is < 1 or > 50 || query.After == Guid.Empty
            ? Task.FromResult(Result.Failure<GroupPage>(Error.Validation("Common.ValidationFailed", "Use a limit from 1 to 50 and a valid cursor.")))
            : store.ListAsync(query, token);
}
