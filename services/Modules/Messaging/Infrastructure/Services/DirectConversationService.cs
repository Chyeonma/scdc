using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;
using SCDC.Contracts.Messaging;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Domain;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class DirectConversationService(
    MessagingDbContext dbContext,
    IUserDirectory userDirectory,
    IUnreadCountReader unreadCountReader,
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
            return await ToExistingResultAsync(existing, recipient, command.ActorUserId, cancellationToken);
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
                return await ToExistingResultAsync(concurrentConversation, recipient, command.ActorUserId, cancellationToken);
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

        var unread = await unreadCountReader.GetAsync(actorUserId, [spaceId], cancellationToken);
        return Result.Success(ToSpaceSummary(directConversation.Space, peer, state, unread[spaceId].UnreadCount));
    }

    public async Task<Result<SpacePageDto>> ListAsync(
        ListSpacesQuery query,
        CancellationToken cancellationToken)
    {
        if (query.ActorUserId == Guid.Empty)
        {
            return Result.Failure<SpacePageDto>(MessagingErrors.AccountUnavailable);
        }

        if (query.Limit is < 1 or > 100)
        {
            return Result.Failure<SpacePageDto>(MessagingErrors.InvalidPageSize);
        }

        var actor = await userDirectory.FindByIdAsync(query.ActorUserId, cancellationToken);
        if (actor is null)
        {
            return Result.Failure<SpacePageDto>(MessagingErrors.AccountUnavailable);
        }

        if (!TryDecodeCursor(query.Cursor, query.IncludeHidden, out var cursor))
        {
            return Result.Failure<SpacePageDto>(MessagingErrors.InvalidCursor);
        }

        var inboxQuery =
            from conversation in dbContext.DirectConversations.AsNoTracking()
            join space in dbContext.Spaces.AsNoTracking() on conversation.SpaceId equals space.Id
            join member in dbContext.SpaceMembers.AsNoTracking() on space.Id equals member.SpaceId
            join userState in dbContext.SpaceUserStates.AsNoTracking()
                on new { SpaceId = space.Id, UserId = query.ActorUserId }
                equals new { userState.SpaceId, userState.UserId } into userStates
            from state in userStates.DefaultIfEmpty()
            where space.SpaceType == SpaceType.Direct
                  && space.Status != SpaceStatus.Deleted
                  && member.UserId == query.ActorUserId
                  && member.MembershipStatus == SpaceMembershipStatus.Active
                  && (query.IncludeHidden || state == null || !state.IsHidden)
            select new { Conversation = conversation, Space = space, State = state };

        if (cursor is not null)
        {
            inboxQuery = cursor.LastActivityAt is { } lastActivityAt
                ? inboxQuery.Where(row =>
                    row.Space.LastActivityAt == null
                    || row.Space.LastActivityAt < lastActivityAt
                    || (row.Space.LastActivityAt == lastActivityAt
                        && row.Space.Id.CompareTo(cursor.SpaceId) < 0))
                : inboxQuery.Where(row =>
                    row.Space.LastActivityAt == null
                    && row.Space.Id.CompareTo(cursor.SpaceId) < 0);
        }

        var rows = (await inboxQuery
            .OrderByDescending(row => row.Space.LastActivityAt.HasValue)
            .ThenByDescending(row => row.Space.LastActivityAt)
            .ThenByDescending(row => row.Space.Id)
            .Take(query.Limit + 1)
            .ToListAsync(cancellationToken))
            .Select(row => new DirectInboxRow(row.Conversation, row.Space, row.State))
            .ToArray();

        var hasMore = rows.Length > query.Limit;
        var pageRows = rows.Take(query.Limit).ToArray();
        var peerIds = pageRows
            .Select(row => GetPeerUserId(row.Conversation, query.ActorUserId))
            .Distinct()
            .ToArray();
        var peers = await userDirectory.FindByIdsAsync(peerIds, cancellationToken);
        var unread = await unreadCountReader.GetAsync(query.ActorUserId, pageRows.Select(row => row.Space.Id).ToArray(), cancellationToken);

        var items = pageRows
            .Where(row => peers.ContainsKey(GetPeerUserId(row.Conversation, query.ActorUserId)))
            .Select(row => ToSpaceSummary(
                row.Space,
                peers[GetPeerUserId(row.Conversation, query.ActorUserId)],
                row.State,
                unread[row.Space.Id].UnreadCount))
            .ToArray();

        var nextCursor = hasMore && pageRows.Length > 0
            ? EncodeCursor(pageRows[^1], query.IncludeHidden)
            : null;

        return Result.Success(new SpacePageDto(items, nextCursor, hasMore));
    }

    public async Task<Result<UserSummary>> FindRecipientByUsernameAsync(
        Guid actorUserId,
        string username,
        CancellationToken cancellationToken)
    {
        if (actorUserId == Guid.Empty)
        {
            return Result.Failure<UserSummary>(MessagingErrors.AccountUnavailable);
        }

        var actor = await userDirectory.FindByIdAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            return Result.Failure<UserSummary>(MessagingErrors.AccountUnavailable);
        }

        var recipient = await userDirectory.FindByUsernameAsync(username, cancellationToken);
        if (recipient is null)
        {
            return Result.Failure<UserSummary>(MessagingErrors.DirectConversationUnavailable);
        }

        if (recipient.Id == actorUserId)
        {
            return Result.Failure<UserSummary>(MessagingErrors.SelfConversationNotAllowed);
        }

        if (await IsBlockedAsync(actorUserId, recipient.Id, cancellationToken))
        {
            return Result.Failure<UserSummary>(MessagingErrors.DirectConversationUnavailable);
        }

        return Result.Success(recipient);
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

    private static Guid GetPeerUserId(DirectConversation conversation, Guid actorUserId) =>
        conversation.UserLowId == actorUserId ? conversation.UserHighId : conversation.UserLowId;

    private static string EncodeCursor(DirectInboxRow row, bool includeHidden)
    {
        var cursor = new InboxCursor(
            Version: 1,
            LastActivityAt: row.Space.LastActivityAt,
            SpaceId: row.Space.Id,
            IncludeHidden: includeHidden);
        return Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(cursor));
    }

    private static bool TryDecodeCursor(string? value, bool includeHidden, out InboxCursor? cursor)
    {
        cursor = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        try
        {
            var decoded = JsonSerializer.Deserialize<InboxCursor>(Base64UrlDecode(value));
            if (decoded is null
                || decoded.Version != 1
                || decoded.SpaceId == Guid.Empty
                || decoded.IncludeHidden != includeHidden)
            {
                return false;
            }

            cursor = decoded;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string Base64UrlEncode(byte[] bytes) => Convert.ToBase64String(bytes)
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        normalized = normalized.PadRight(normalized.Length + (4 - normalized.Length % 4) % 4, '=');
        return Convert.FromBase64String(normalized);
    }

    private async Task<Result<CreateDirectConversationResult>> ToExistingResultAsync(
        DirectConversationWithSpace directConversation,
        UserSummary recipient,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (directConversation.Space.Status == SpaceStatus.Deleted)
        {
            return Result.Failure<CreateDirectConversationResult>(MessagingErrors.DirectConversationClosed);
        }

        var state = await dbContext.SpaceUserStates.AsNoTracking().SingleOrDefaultAsync(
            item => item.SpaceId == directConversation.Space.Id && item.UserId == actorUserId,
            cancellationToken);
        var unread = await unreadCountReader.GetAsync(actorUserId, [directConversation.Space.Id], cancellationToken);
        return Result.Success(new CreateDirectConversationResult(
            ToSpaceSummary(directConversation.Space, recipient, state, unread[directConversation.Space.Id].UnreadCount),
            Created: false));
    }

    private static SpaceSummaryDto ToSpaceSummary(
        ChatSpace space,
        UserSummary peer,
        SpaceUserState? state,
        int unreadCount = 0)
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
            UnreadCount: unreadCount,
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

    private sealed record DirectInboxRow(
        DirectConversation Conversation,
        ChatSpace Space,
        SpaceUserState? State);

    private sealed record InboxCursor(
        int Version,
        DateTimeOffset? LastActivityAt,
        Guid SpaceId,
        bool IncludeHidden);
}
