using Microsoft.EntityFrameworkCore;
using Npgsql;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Domain;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class DirectConversationService(
    MessagingDbContext dbContext,
    IUserDirectory userDirectory,
    TimeProvider timeProvider) : IDirectConversationService
{
    public async Task<Result<CreateDirectConversationResult>> GetOrCreateAsync(
        CreateDirectConversationCommand command,
        CancellationToken cancellationToken)
    {
        if (command.RecipientUserId == Guid.Empty)
        {
            return Result.Failure<CreateDirectConversationResult>(MessagingErrors.InvalidRecipient);
        }

        if (command.ActorUserId == Guid.Empty)
        {
            return Result.Failure<CreateDirectConversationResult>(MessagingErrors.AccountUnavailable);
        }

        if (command.ActorUserId == command.RecipientUserId)
        {
            return Result.Failure<CreateDirectConversationResult>(MessagingErrors.SelfConversationNotAllowed);
        }

        var actor = await userDirectory.FindByIdAsync(command.ActorUserId, cancellationToken);
        if (actor is null)
        {
            return Result.Failure<CreateDirectConversationResult>(MessagingErrors.AccountUnavailable);
        }

        var recipient = await userDirectory.FindByIdAsync(command.RecipientUserId, cancellationToken);
        if (recipient is null)
        {
            return Result.Failure<CreateDirectConversationResult>(MessagingErrors.DirectConversationUnavailable);
        }

        var (userLowId, userHighId) = OrderUserIds(command.ActorUserId, command.RecipientUserId);

        if (await IsBlockedAsync(command.ActorUserId, command.RecipientUserId, cancellationToken))
        {
            return Result.Failure<CreateDirectConversationResult>(MessagingErrors.DirectConversationUnavailable);
        }

        var existing = await FindDirectConversationAsync(userLowId, userHighId, cancellationToken);
        if (existing is not null)
        {
            return ToExistingResult(existing, recipient);
        }

        var now = timeProvider.GetUtcNow();
        var spaceId = Guid.CreateVersion7();
        var space = new ChatSpace
        {
            Id = spaceId,
            SpaceType = SpaceType.Direct,
            Status = SpaceStatus.Active,
            CreatedByUserId = command.ActorUserId,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1
        };

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            dbContext.Spaces.Add(space);
            dbContext.DirectConversations.Add(new DirectConversation
            {
                SpaceId = spaceId,
                UserLowId = userLowId,
                UserHighId = userHighId,
                CreatedAt = now
            });
            dbContext.SpaceMembers.AddRange(
                CreateActiveMember(spaceId, command.ActorUserId, now),
                CreateActiveMember(spaceId, command.RecipientUserId, now));
            dbContext.SpaceUserStates.AddRange(
                CreateDefaultState(spaceId, command.ActorUserId, now),
                CreateDefaultState(spaceId, command.RecipientUserId, now));

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result.Success(new CreateDirectConversationResult(
                ToSpaceSummary(space, recipient, null),
                Created: true));
        }
        catch (DbUpdateException exception) when (IsDirectConversationPairConflict(exception))
        {
            dbContext.ChangeTracker.Clear();
            var concurrentConversation = await FindDirectConversationAsync(userLowId, userHighId, cancellationToken);
            if (concurrentConversation is not null)
            {
                return ToExistingResult(concurrentConversation, recipient);
            }

            return Result.Failure<CreateDirectConversationResult>(MessagingErrors.DirectConversationConflict);
        }
    }

    public async Task<Result<SpaceSummaryDto>> GetSpaceAsync(
        Guid actorUserId,
        Guid spaceId,
        CancellationToken cancellationToken)
    {
        if (actorUserId == Guid.Empty)
        {
            return Result.Failure<SpaceSummaryDto>(MessagingErrors.AccountUnavailable);
        }

        var actor = await userDirectory.FindByIdAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            return Result.Failure<SpaceSummaryDto>(MessagingErrors.AccountUnavailable);
        }

        var directConversation = await (
            from conversation in dbContext.DirectConversations.AsNoTracking()
            join space in dbContext.Spaces.AsNoTracking() on conversation.SpaceId equals space.Id
            join member in dbContext.SpaceMembers.AsNoTracking() on space.Id equals member.SpaceId
            where space.Id == spaceId
                  && space.SpaceType == SpaceType.Direct
                  && space.Status != SpaceStatus.Deleted
                  && member.UserId == actorUserId
                  && member.MembershipStatus == SpaceMembershipStatus.Active
            select new DirectConversationWithSpace(conversation, space))
            .SingleOrDefaultAsync(cancellationToken);

        if (directConversation is null)
        {
            return Result.Failure<SpaceSummaryDto>(MessagingErrors.ResourceNotFound);
        }

        var peerUserId = directConversation.Conversation.UserLowId == actorUserId
            ? directConversation.Conversation.UserHighId
            : directConversation.Conversation.UserLowId;
        var peer = await userDirectory.FindByIdAsync(peerUserId, cancellationToken);
        if (peer is null)
        {
            return Result.Failure<SpaceSummaryDto>(MessagingErrors.ResourceNotFound);
        }

        var state = await dbContext.SpaceUserStates
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.SpaceId == spaceId && item.UserId == actorUserId,
                cancellationToken);

        return Result.Success(ToSpaceSummary(directConversation.Space, peer, state));
    }

    private async Task<DirectConversationWithSpace?> FindDirectConversationAsync(
        Guid userLowId,
        Guid userHighId,
        CancellationToken cancellationToken) => await (
            from conversation in dbContext.DirectConversations.AsNoTracking()
            join space in dbContext.Spaces.AsNoTracking() on conversation.SpaceId equals space.Id
            where conversation.UserLowId == userLowId && conversation.UserHighId == userHighId
            select new DirectConversationWithSpace(conversation, space))
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<bool> IsBlockedAsync(
        Guid actorUserId,
        Guid recipientUserId,
        CancellationToken cancellationToken) => await dbContext.UserBlocks
            .AsNoTracking()
            .AnyAsync(
                block => (block.BlockerUserId == actorUserId && block.BlockedUserId == recipientUserId)
                         || (block.BlockerUserId == recipientUserId && block.BlockedUserId == actorUserId),
                cancellationToken);

    private static Result<CreateDirectConversationResult> ToExistingResult(
        DirectConversationWithSpace directConversation,
        UserSummary recipient)
    {
        if (directConversation.Space.Status == SpaceStatus.Deleted)
        {
            return Result.Failure<CreateDirectConversationResult>(MessagingErrors.DirectConversationClosed);
        }

        return Result.Success(new CreateDirectConversationResult(
            ToSpaceSummary(directConversation.Space, recipient, null),
            Created: false));
    }

    private static SpaceSummaryDto ToSpaceSummary(
        ChatSpace space,
        UserSummary peer,
        SpaceUserState? state)
    {
        return new SpaceSummaryDto(
            space.Id,
            (short)space.SpaceType,
            (short)space.Status,
            space.Version,
            peer.DisplayName,
            peer,
            ServerId: null,
            LastMessage: null,
            LastMessageSequence: space.LastMessageSequence?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            LastActivityAt: space.LastActivityAt,
            LastReadSequence: state?.LastReadSequence?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            UnreadCount: 0,
            new SpacePreferencesDto(
                (short)(state?.NotificationLevel ?? NotificationLevel.AllMessages),
                state?.MutedUntil,
                state?.IsHidden ?? false,
                state?.IsPinned ?? false),
            new SpaceCapabilitiesDto(
                CanRead: true,
                CanSend: space.Status == SpaceStatus.Active,
                CanEditOwn: space.Status == SpaceStatus.Active,
                CanDeleteOwn: true,
                CanDeleteOthers: false,
                CanPin: space.Status == SpaceStatus.Active,
                CanReact: space.Status == SpaceStatus.Active,
                CanAttach: space.Status == SpaceStatus.Active));
    }

    private static SpaceMember CreateActiveMember(Guid spaceId, Guid userId, DateTimeOffset now) => new()
    {
        SpaceId = spaceId,
        UserId = userId,
        MemberRole = SpaceMemberRole.Member,
        MembershipStatus = SpaceMembershipStatus.Active,
        JoinedAt = now
    };

    private static SpaceUserState CreateDefaultState(Guid spaceId, Guid userId, DateTimeOffset now) => new()
    {
        SpaceId = spaceId,
        UserId = userId,
        NotificationLevel = NotificationLevel.AllMessages,
        UpdatedAt = now
    };

    private static (Guid Low, Guid High) OrderUserIds(Guid first, Guid second) =>
        ComparePostgresUuidOrder(first, second) < 0 ? (first, second) : (second, first);

    private static int ComparePostgresUuidOrder(Guid first, Guid second)
    {
        var firstBytes = first.ToByteArray(bigEndian: true);
        var secondBytes = second.ToByteArray(bigEndian: true);
        for (var index = 0; index < firstBytes.Length; index++)
        {
            var comparison = firstBytes[index].CompareTo(secondBytes[index]);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return 0;
    }

    private static bool IsDirectConversationPairConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ux_direct_conversations_pair"
        };

    private sealed record DirectConversationWithSpace(DirectConversation Conversation, ChatSpace Space);
}
