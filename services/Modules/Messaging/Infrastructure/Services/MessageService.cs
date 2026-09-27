using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Domain;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class MessageService(
    MessagingDbContext dbContext,
    IUserDirectory userDirectory,
    SpaceMessageAccess spaceAccess,
    IRealtimeSpaceAccess realtimeSpaceAccess,
    MessageRateLimiter rateLimiter,
    TimeProvider timeProvider) : IMessageService
{
    private static readonly Regex MentionPattern = new(
        @"(?<![A-Za-z0-9_.@])@([A-Za-z0-9_.]{3,32})(?![A-Za-z0-9_.])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public async Task<Result<SendMessageResult>> SendAsync(
        SendMessageCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ActorUserId == Guid.Empty || command.SpaceId == Guid.Empty || command.ClientMessageId == Guid.Empty
            || command.ReplyToMessageId == Guid.Empty || command.ThreadRootId == Guid.Empty)
        {
            return Result.Failure<SendMessageResult>(MessagingErrors.InvalidMessage);
        }

        if (command.MessageType != (short)MessageType.Text
            || !TryNormalizeText(command.Content, out var content))
        {
            return Result.Failure<SendMessageResult>(MessagingErrors.InvalidMessage);
        }

        if (!rateLimiter.TryAcquire(command.ActorUserId))
        {
            return Result.Failure<SendMessageResult>(MessagingErrors.RateLimited);
        }

        var actor = await userDirectory.FindByIdAsync(command.ActorUserId, cancellationToken);
        if (actor is null)
        {
            return Result.Failure<SendMessageResult>(MessagingErrors.AccountUnavailable);
        }

        var effectiveReplyId = command.ReplyToMessageId ?? command.ThreadRootId;
        var payloadHash = ComputePayloadHash(MessageType.Text, content, effectiveReplyId, command.ThreadRootId);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var space = await dbContext.Spaces
            .FromSqlInterpolated($"SELECT * FROM messaging.spaces WHERE id = {command.SpaceId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (space is null || space.Status == SpaceStatus.Deleted)
        {
            return Result.Failure<SendMessageResult>(MessagingErrors.ResourceNotFound);
        }

        var access = await spaceAccess.CheckAsync(command.ActorUserId, space, cancellationToken);
        if (!access.CanRead)
            return Result.Failure<SendMessageResult>(MessagingErrors.ResourceNotFound);
        if (space.Status != SpaceStatus.Active)
            return Result.Failure<SendMessageResult>(MessagingErrors.SpaceNotWritable);
        if (!access.CanSend)
            return Result.Failure<SendMessageResult>(MessagingErrors.ActionNotAllowed);

        if (space.SpaceType == SpaceType.Direct)
        {
            var directConversation = await dbContext.DirectConversations.AsNoTracking().SingleAsync(
                conversation => conversation.SpaceId == command.SpaceId, cancellationToken);
            var peerUserId = directConversation.UserLowId == command.ActorUserId
                ? directConversation.UserHighId
                : directConversation.UserLowId;
            if (await userDirectory.FindByIdAsync(peerUserId, cancellationToken) is null
                || await IsBlockedAsync(command.ActorUserId, peerUserId, cancellationToken))
                return Result.Failure<SendMessageResult>(MessagingErrors.ActionNotAllowed);
        }

        var existing = await dbContext.Messages
            .AsNoTracking()
            .SingleOrDefaultAsync(message => message.SpaceId == command.SpaceId
                                             && message.AuthorUserId == command.ActorUserId
                                             && message.ClientMessageId == command.ClientMessageId,
                cancellationToken);
        if (existing is not null)
        {
            return existing.IdempotencyPayloadHash == payloadHash
                ? Result.Success(new SendMessageResult((await ToDtosAsync([existing], cancellationToken))[0], Created: false))
                : Result.Failure<SendMessageResult>(MessagingErrors.IdempotencyConflict);
        }

        if (effectiveReplyId is { } replyId)
        {
            var replyTarget = await dbContext.Messages.AsNoTracking().SingleOrDefaultAsync(
                message => message.Id == replyId && message.SpaceId == command.SpaceId, cancellationToken);
            if (replyTarget is null) return Result.Failure<SendMessageResult>(MessagingErrors.ResourceNotFound);
            if (replyTarget.DeletedAt is not null) return Result.Failure<SendMessageResult>(MessagingErrors.MessageDeleted);
            if (replyTarget.MessageType is not (MessageType.Text or MessageType.Attachment))
                return Result.Failure<SendMessageResult>(MessagingErrors.InvalidMessage);

            if (command.ThreadRootId is { } rootId)
            {
                var root = rootId == replyTarget.Id ? replyTarget : await dbContext.Messages.AsNoTracking()
                    .SingleOrDefaultAsync(message => message.Id == rootId && message.SpaceId == command.SpaceId, cancellationToken);
                if (root is null) return Result.Failure<SendMessageResult>(MessagingErrors.ResourceNotFound);
                if (root.DeletedAt is not null) return Result.Failure<SendMessageResult>(MessagingErrors.MessageDeleted);
                if (root.ThreadRootId is not null || root.MessageType is not (MessageType.Text or MessageType.Attachment)
                    || (replyTarget.Id != rootId && replyTarget.ThreadRootId != rootId))
                    return Result.Failure<SendMessageResult>(MessagingErrors.InvalidMessage);
            }
            else if (replyTarget.ThreadRootId is not null)
            {
                return Result.Failure<SendMessageResult>(MessagingErrors.InvalidMessage);
            }
        }

        var now = timeProvider.GetUtcNow();
        var mentions = await ResolveMentionsAsync(command.SpaceId, command.ActorUserId, content, cancellationToken);
        var message = new Message
        {
            Id = Guid.CreateVersion7(),
            SpaceId = command.SpaceId,
            AuthorUserId = command.ActorUserId,
            ClientMessageId = command.ClientMessageId,
            ReplyToMessageId = effectiveReplyId,
            ThreadRootId = command.ThreadRootId,
            MessageType = MessageType.Text,
            Content = content,
            IdempotencyPayloadHash = payloadHash,
            Version = 1,
            CreatedAt = now
        };
        dbContext.Messages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.MessageMentions.AddRange(mentions.Select(user => new MessageMention
        {
            MessageId = message.Id, MentionedUserId = user.Id, CreatedAt = now
        }));

        if (command.ThreadRootId is null)
        {
            space.LastMessageId = message.Id;
            space.LastMessageSequence = message.SequenceNo;
            space.LastActivityAt = now;
            // Thread replies stay out of inbox activity and do not unhide conversations.
            await dbContext.SpaceUserStates
                .Where(state => state.SpaceId == command.SpaceId
                                && state.UserId != command.ActorUserId
                                && state.IsHidden)
                .ExecuteUpdateAsync(setters => setters.SetProperty(state => state.IsHidden, false), cancellationToken);
        }
        dbContext.OutboxEvents.Add(new OutboxEvent
        {
            Id = Guid.CreateVersion7(),
            EventType = "Messaging.MessageCreated",
            AggregateType = "Message",
            AggregateId = message.Id,
            AggregateVersion = message.Version,
            SpaceId = command.SpaceId,
            Payload = JsonSerializer.Serialize(new
            {
                messageId = message.Id,
                sequenceNo = message.SequenceNo.ToString(CultureInfo.InvariantCulture)
            }),
            OccurredAt = now,
            AvailableAt = now,
            AttemptCount = 0
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new SendMessageResult((await ToDtosAsync([message], cancellationToken))[0], Created: true));
    }

    public async Task<Result<MessagePageDto>> GetHistoryAsync(
        GetMessagesQuery query,
        CancellationToken cancellationToken)
    {
        if (query.ActorUserId == Guid.Empty || query.SpaceId == Guid.Empty || query.Limit is < 1 or > 100)
        {
            return Result.Failure<MessagePageDto>(MessagingErrors.InvalidMessage);
        }

        if (!TryParseCursor(query.BeforeSequence, allowZero: false, out var before)
            || !TryParseCursor(query.AfterSequence, allowZero: true, out var after)
            || !TryParseCursor(query.ThroughSequence, allowZero: true, out var through)
            || (before is not null && after is not null)
            || (through is not null && after is null)
            || (after is not null && through is not null && after > through))
        {
            return Result.Failure<MessagePageDto>(MessagingErrors.InvalidMessageCursor);
        }

        var actor = await userDirectory.FindByIdAsync(query.ActorUserId, cancellationToken);
        if (actor is null)
        {
            return Result.Failure<MessagePageDto>(MessagingErrors.AccountUnavailable);
        }

        var space = await dbContext.Spaces.AsNoTracking().SingleOrDefaultAsync(x => x.Id == query.SpaceId && x.Status != SpaceStatus.Deleted, cancellationToken);
        if (space is null) return Result.Failure<MessagePageDto>(MessagingErrors.ResourceNotFound);
        if (!(await spaceAccess.CheckAsync(query.ActorUserId, space, cancellationToken)).CanRead)
        {
            return Result.Failure<MessagePageDto>(MessagingErrors.ResourceNotFound);
        }

        var highWatermark = await dbContext.Messages
            .AsNoTracking()
            .Where(message => message.SpaceId == query.SpaceId)
            .Select(message => (long?)message.SequenceNo)
            .MaxAsync(cancellationToken) ?? 0;

        if (after is not null)
        {
            var throughSequence = through ?? highWatermark;
            var rows = await dbContext.Messages
                .AsNoTracking()
                .Where(message => message.SpaceId == query.SpaceId
                                  && message.SequenceNo > after
                                  && message.SequenceNo <= throughSequence)
                .OrderBy(message => message.SequenceNo)
                .Take(query.Limit + 1)
                .ToListAsync(cancellationToken);
            var hasMore = rows.Count > query.Limit;
            var pageRows = rows.Take(query.Limit).ToArray();
            var items = await ToDtosAsync(pageRows, cancellationToken);
            return Result.Success(new MessagePageDto(
                items,
                hasMore,
                NextBeforeSequence: null,
                NextAfterSequence: hasMore ? ToSequence(pageRows[^1].SequenceNo) : null,
                HighWatermark: ToSequence(throughSequence)));
        }

        var historyQuery = dbContext.Messages
            .AsNoTracking()
            .Where(message => message.SpaceId == query.SpaceId);
        if (before is not null)
        {
            historyQuery = historyQuery.Where(message => message.SequenceNo < before);
        }

        var historyRows = await historyQuery
            .OrderByDescending(message => message.SequenceNo)
            .Take(query.Limit + 1)
            .ToListAsync(cancellationToken);
        var historyHasMore = historyRows.Count > query.Limit;
        var historyPageRows = historyRows.Take(query.Limit)
            .OrderBy(message => message.SequenceNo)
            .ToArray();
        var historyItems = await ToDtosAsync(historyPageRows, cancellationToken);
        return Result.Success(new MessagePageDto(
            historyItems,
            historyHasMore,
            NextBeforeSequence: historyHasMore ? ToSequence(historyPageRows[0].SequenceNo) : null,
            NextAfterSequence: null,
            HighWatermark: ToSequence(highWatermark)));
    }

    public async Task<Result<MessageSearchPageDto>> SearchAsync(SearchMessagesQuery query,
        CancellationToken cancellationToken)
    {
        var text = string.IsNullOrWhiteSpace(query.Text) ? null : query.Text.Trim();
        if (query.ActorUserId == Guid.Empty || query.SpaceId == Guid.Empty
            || query.AuthorUserId == Guid.Empty || query.Limit is < 1 or > 50
            || text is { Length: < 2 or > 120 }
            || text?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > 12
            || (query.From is null) != (query.To is null)
            || query.From > query.To
            || query.To - query.From > TimeSpan.FromDays(366)
            || (string.IsNullOrWhiteSpace(text) && query.AuthorUserId is null && query.From is null))
            return Result.Failure<MessageSearchPageDto>(MessagingErrors.InvalidSearch);
        if (!TryParseCursor(query.BeforeSequence, allowZero: false, out var before))
            return Result.Failure<MessageSearchPageDto>(MessagingErrors.InvalidMessageCursor);
        if (await userDirectory.FindByIdAsync(query.ActorUserId, cancellationToken) is null)
            return Result.Failure<MessageSearchPageDto>(MessagingErrors.AccountUnavailable);
        var space = await dbContext.Spaces.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == query.SpaceId && item.Status != SpaceStatus.Deleted, cancellationToken);
        if (space is null || !(await spaceAccess.CheckAsync(query.ActorUserId, space, cancellationToken)).CanRead)
            return Result.Failure<MessageSearchPageDto>(MessagingErrors.ResourceNotFound);

        // Target the generated vector; PostgreSQL can use its partial GIN index.
        IQueryable<Message> matches = string.IsNullOrWhiteSpace(text)
            ? dbContext.Messages.AsNoTracking()
            : dbContext.Messages.FromSqlInterpolated($"SELECT * FROM messaging.messages WHERE deleted_at IS NULL AND search_vector @@ plainto_tsquery('simple', {text})").AsNoTracking();
        matches = matches.Where(message => message.SpaceId == query.SpaceId
            && message.DeletedAt == null && message.MessageType != MessageType.System);
        if (query.AuthorUserId is { } authorId)
            matches = matches.Where(message => message.AuthorUserId == authorId);
        if (query.From is { } from)
            matches = matches.Where(message => message.CreatedAt >= from);
        if (query.To is { } to)
            matches = matches.Where(message => message.CreatedAt < to);
        if (before is { } sequence)
            matches = matches.Where(message => message.SequenceNo < sequence);

        var rows = await matches.OrderByDescending(message => message.SequenceNo)
            .Take(query.Limit + 1).ToListAsync(cancellationToken);
        var hasMore = rows.Count > query.Limit;
        var page = rows.Take(query.Limit).ToArray();
        return Result.Success(new MessageSearchPageDto(await ToDtosAsync(page, cancellationToken),
            hasMore, hasMore ? ToSequence(page[^1].SequenceNo) : null));
    }

    public async Task<Result<MessageDto>> GetAsync(Guid actorUserId, Guid spaceId, Guid messageId, CancellationToken cancellationToken)
    {
        if (actorUserId == Guid.Empty || spaceId == Guid.Empty || messageId == Guid.Empty)
            return Result.Failure<MessageDto>(MessagingErrors.ResourceNotFound);
        if (await userDirectory.FindByIdAsync(actorUserId, cancellationToken) is null)
            return Result.Failure<MessageDto>(MessagingErrors.AccountUnavailable);
        var space = await dbContext.Spaces.AsNoTracking().SingleOrDefaultAsync(x => x.Id == spaceId && x.Status != SpaceStatus.Deleted, cancellationToken);
        if (space is null || !(await spaceAccess.CheckAsync(actorUserId, space, cancellationToken)).CanRead)
            return Result.Failure<MessageDto>(MessagingErrors.ResourceNotFound);
        var message = await dbContext.Messages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == messageId && x.SpaceId == spaceId, cancellationToken);
        if (message is null) return Result.Failure<MessageDto>(MessagingErrors.ResourceNotFound);
        return Result.Success((await ToDtosAsync([message], cancellationToken))[0]);
    }

    public async Task<Result<IReadOnlyList<MentionSummaryDto>>> SuggestMentionsAsync(
        Guid actorUserId, Guid spaceId, string? query, CancellationToken cancellationToken)
    {
        if (actorUserId == Guid.Empty || spaceId == Guid.Empty || await userDirectory.FindByIdAsync(actorUserId, cancellationToken) is null)
            return Result.Failure<IReadOnlyList<MentionSummaryDto>>(MessagingErrors.ResourceNotFound);
        var space = await dbContext.Spaces.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == spaceId && item.Status != SpaceStatus.Deleted, cancellationToken);
        if (space is null || !(await spaceAccess.CheckAsync(actorUserId, space, cancellationToken)).CanRead)
            return Result.Failure<IReadOnlyList<MentionSummaryDto>>(MessagingErrors.ResourceNotFound);
        var prefix = query?.Trim().TrimStart('@') ?? string.Empty;
        if (prefix.Length is < 1 or > 32 || prefix.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '_' and '.'))
            return Result.Failure<IReadOnlyList<MentionSummaryDto>>(MessagingErrors.InvalidMessage);
        var memberIds = await realtimeSpaceAccess.GetActiveMemberIdsAsync(spaceId, cancellationToken);
        var users = await userDirectory.FindByIdsAsync(memberIds, cancellationToken);
        return Result.Success<IReadOnlyList<MentionSummaryDto>>(users.Values
            .Where(user => user.Id != actorUserId && user.Username.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(user => user.Username, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .Select(user => new MentionSummaryDto(user.Id, user.Username)).ToArray());
    }

    public async Task<Result<MessagePageDto>> GetThreadRepliesAsync(GetThreadRepliesQuery query, CancellationToken cancellationToken)
    {
        if (query.ActorUserId == Guid.Empty || query.SpaceId == Guid.Empty || query.RootMessageId == Guid.Empty
            || query.Limit is < 1 or > 100)
            return Result.Failure<MessagePageDto>(MessagingErrors.InvalidMessage);
        if (!TryParseCursor(query.BeforeSequence, allowZero: false, out var before)
            || !TryParseCursor(query.AfterSequence, allowZero: true, out var after)
            || !TryParseCursor(query.ThroughSequence, allowZero: true, out var through)
            || (before is not null && after is not null)
            || (through is not null && after is null)
            || (after is not null && through is not null && after > through))
            return Result.Failure<MessagePageDto>(MessagingErrors.InvalidMessageCursor);
        if (await userDirectory.FindByIdAsync(query.ActorUserId, cancellationToken) is null)
            return Result.Failure<MessagePageDto>(MessagingErrors.AccountUnavailable);
        var space = await dbContext.Spaces.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == query.SpaceId && item.Status != SpaceStatus.Deleted, cancellationToken);
        if (space is null || !(await spaceAccess.CheckAsync(query.ActorUserId, space, cancellationToken)).CanRead)
            return Result.Failure<MessagePageDto>(MessagingErrors.ResourceNotFound);
        var root = await dbContext.Messages.AsNoTracking().SingleOrDefaultAsync(
            message => message.Id == query.RootMessageId && message.SpaceId == query.SpaceId, cancellationToken);
        if (root is null) return Result.Failure<MessagePageDto>(MessagingErrors.ResourceNotFound);
        if (root.ThreadRootId is not null || root.MessageType is not (MessageType.Text or MessageType.Attachment))
            return Result.Failure<MessagePageDto>(MessagingErrors.InvalidMessage);

        var replies = dbContext.Messages.AsNoTracking()
            .Where(message => message.SpaceId == query.SpaceId && message.ThreadRootId == query.RootMessageId);
        var highWatermark = await replies.Select(message => (long?)message.SequenceNo).MaxAsync(cancellationToken) ?? 0;
        if (after is not null)
        {
            var boundary = through ?? highWatermark;
            var rows = await replies.Where(message => message.SequenceNo > after && message.SequenceNo <= boundary)
                .OrderBy(message => message.SequenceNo).Take(query.Limit + 1).ToListAsync(cancellationToken);
            var page = rows.Take(query.Limit).ToArray();
            return Result.Success(new MessagePageDto(await ToDtosAsync(page, cancellationToken),
                rows.Count > query.Limit, null,
                rows.Count > query.Limit ? ToSequence(page[^1].SequenceNo) : null, ToSequence(boundary)));
        }
        if (before is not null) replies = replies.Where(message => message.SequenceNo < before);
        var historyRows = await replies.OrderByDescending(message => message.SequenceNo)
            .Take(query.Limit + 1).ToListAsync(cancellationToken);
        var historyPage = historyRows.Take(query.Limit).OrderBy(message => message.SequenceNo).ToArray();
        return Result.Success(new MessagePageDto(await ToDtosAsync(historyPage, cancellationToken),
            historyRows.Count > query.Limit,
            historyRows.Count > query.Limit ? ToSequence(historyPage[0].SequenceNo) : null,
            null, ToSequence(highWatermark)));
    }

    public async Task<Result<MessageDto>> EditAsync(EditMessageCommand command, CancellationToken cancellationToken)
    {
        if (command.ActorUserId == Guid.Empty || command.SpaceId == Guid.Empty || command.MessageId == Guid.Empty
            || command.ExpectedVersion < 1 || !TryNormalizeText(command.Content, out var content))
            return Result.Failure<MessageDto>(MessagingErrors.InvalidMessage);
        var actor = await userDirectory.FindByIdAsync(command.ActorUserId, cancellationToken);
        if (actor is null) return Result.Failure<MessageDto>(MessagingErrors.AccountUnavailable);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var space = await dbContext.Spaces
            .FromSqlInterpolated($"SELECT * FROM messaging.spaces WHERE id = {command.SpaceId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (space is null || space.Status == SpaceStatus.Deleted)
            return Result.Failure<MessageDto>(MessagingErrors.ResourceNotFound);
        var access = await spaceAccess.CheckAsync(command.ActorUserId, space, cancellationToken);
        if (!access.CanRead) return Result.Failure<MessageDto>(MessagingErrors.ResourceNotFound);
        var message = await dbContext.Messages.SingleOrDefaultAsync(x => x.Id == command.MessageId && x.SpaceId == command.SpaceId, cancellationToken);
        if (message is null) return Result.Failure<MessageDto>(MessagingErrors.ResourceNotFound);
        if (message.AuthorUserId != command.ActorUserId || message.MessageType != MessageType.Text)
            return Result.Failure<MessageDto>(MessagingErrors.ActionNotAllowed);
        if (message.DeletedAt is not null) return Result.Failure<MessageDto>(MessagingErrors.MessageDeleted);
        if (space.Status != SpaceStatus.Active) return Result.Failure<MessageDto>(MessagingErrors.SpaceNotWritable);
        if (!access.CanSend || (space.SpaceType == SpaceType.Channel && !access.CanEditOwn))
            return Result.Failure<MessageDto>(MessagingErrors.ActionNotAllowed);
        if (space.SpaceType == SpaceType.Direct)
        {
            var pair = await dbContext.DirectConversations.AsNoTracking().SingleAsync(x => x.SpaceId == command.SpaceId, cancellationToken);
            var peer = pair.UserLowId == command.ActorUserId ? pair.UserHighId : pair.UserLowId;
            if (await IsBlockedAsync(command.ActorUserId, peer, cancellationToken))
                return Result.Failure<MessageDto>(MessagingErrors.ActionNotAllowed);
        }
        if (message.Version != command.ExpectedVersion)
            return Result.Failure<MessageDto>(MessagingErrors.VersionConflict);
        if (message.Content == content) return Result.Success((await ToDtosAsync([message], cancellationToken))[0]);

        var now = timeProvider.GetUtcNow();
        var mentions = await ResolveMentionsAsync(command.SpaceId, command.ActorUserId, content, cancellationToken);
        var existingMentions = await dbContext.MessageMentions
            .Where(mention => mention.MessageId == message.Id).ToListAsync(cancellationToken);
        var wantedIds = mentions.Select(user => user.Id).ToHashSet();
        dbContext.MessageMentions.RemoveRange(existingMentions.Where(mention => !wantedIds.Contains(mention.MentionedUserId)));
        var existingIds = existingMentions.Select(mention => mention.MentionedUserId).ToHashSet();
        dbContext.MessageMentions.AddRange(mentions.Where(user => !existingIds.Contains(user.Id))
            .Select(user => new MessageMention { MessageId = message.Id, MentionedUserId = user.Id, CreatedAt = now }));
        dbContext.MessageEdits.Add(new MessageEdit
        {
            Id = Guid.CreateVersion7(), MessageId = message.Id, Version = message.Version,
            PreviousContent = message.Content, EditedByUserId = command.ActorUserId, EditedAt = now
        });
        message.Content = content;
        message.Version++;
        message.EditedAt = now;
        AddChangeEvent(message, "Messaging.MessageUpdated", now);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success((await ToDtosAsync([message], cancellationToken))[0]);
    }

    public async Task<Result> DeleteAsync(DeleteMessageCommand command, CancellationToken cancellationToken)
    {
        if (command.ActorUserId == Guid.Empty || command.SpaceId == Guid.Empty || command.MessageId == Guid.Empty
            || command.ExpectedVersion < 1)
            return Result.Failure(MessagingErrors.InvalidMessage);
        if (await userDirectory.FindByIdAsync(command.ActorUserId, cancellationToken) is null)
            return Result.Failure(MessagingErrors.AccountUnavailable);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var space = await dbContext.Spaces
            .FromSqlInterpolated($"SELECT * FROM messaging.spaces WHERE id = {command.SpaceId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (space is null || space.Status == SpaceStatus.Deleted)
            return Result.Failure(MessagingErrors.ResourceNotFound);
        var access = await spaceAccess.CheckAsync(command.ActorUserId, space, cancellationToken);
        if (!access.CanRead) return Result.Failure(MessagingErrors.ResourceNotFound);
        var message = await dbContext.Messages.SingleOrDefaultAsync(x => x.Id == command.MessageId && x.SpaceId == command.SpaceId, cancellationToken);
        if (message is null) return Result.Failure(MessagingErrors.ResourceNotFound);
        if (message.MessageType == MessageType.System || (message.AuthorUserId != command.ActorUserId
            && !(space.SpaceType == SpaceType.Channel && access.CanDeleteOthers)
            && !(space.SpaceType == SpaceType.Group && await dbContext.SpaceMembers.AsNoTracking().AnyAsync(
                member => member.SpaceId == command.SpaceId && member.UserId == command.ActorUserId
                    && member.MembershipStatus == SpaceMembershipStatus.Active && member.MemberRole != SpaceMemberRole.Member,
                cancellationToken))))
            return Result.Failure(MessagingErrors.ActionNotAllowed);
        if (message.DeletedAt is not null) return Result.Success();
        if (message.Version != command.ExpectedVersion) return Result.Failure(MessagingErrors.VersionConflict);

        var now = timeProvider.GetUtcNow();
        message.DeletedAt = now;
        message.DeletedByUserId = command.ActorUserId;
        message.Version++;
        // Text is non-null by database constraint; this also removes it from the search vector.
        message.Content = message.MessageType == MessageType.Text ? "[deleted]" : null;
        await dbContext.MessageMentions.Where(mention => mention.MessageId == message.Id)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM messaging.pinned_messages WHERE space_id = {command.SpaceId} AND message_id = {command.MessageId}", cancellationToken);
        AddChangeEvent(message, "Messaging.MessageDeleted", now);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }

    private void AddChangeEvent(Message message, string eventType, DateTimeOffset now)
    {
        dbContext.OutboxEvents.Add(new OutboxEvent
        {
            Id = Guid.CreateVersion7(), EventType = eventType, AggregateType = "Message",
            AggregateId = message.Id, AggregateVersion = message.Version, SpaceId = message.SpaceId,
            Payload = eventType == "Messaging.MessageDeleted"
                ? JsonSerializer.Serialize(new { messageId = message.Id, sequenceNo = ToSequence(message.SequenceNo), deletedAt = message.DeletedAt })
                : JsonSerializer.Serialize(new { messageId = message.Id, sequenceNo = ToSequence(message.SequenceNo) }),
            OccurredAt = now, AvailableAt = now, AttemptCount = 0
        });
    }

    private async Task<bool> IsBlockedAsync(Guid actorUserId, Guid peerUserId, CancellationToken cancellationToken) =>
        await dbContext.UserBlocks
            .AsNoTracking()
            .AnyAsync(block => (block.BlockerUserId == actorUserId && block.BlockedUserId == peerUserId)
                               || (block.BlockerUserId == peerUserId && block.BlockedUserId == actorUserId),
                cancellationToken);

    private async Task<IReadOnlyList<UserSummary>> ResolveMentionsAsync(
        Guid spaceId, Guid actorUserId, string content, CancellationToken cancellationToken)
    {
        var names = MentionPattern.Matches(content).Select(match => match.Groups[1].Value)
            .Where(name => !name.Equals("everyone", StringComparison.OrdinalIgnoreCase)
                           && !name.Equals("here", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToArray();
        if (names.Length == 0) return [];
        var readableIds = (await realtimeSpaceAccess.GetActiveMemberIdsAsync(spaceId, cancellationToken)).ToHashSet();
        var users = new List<UserSummary>();
        foreach (var name in names)
        {
            var user = await userDirectory.FindByUsernameAsync(name, cancellationToken);
            if (user is not null && user.Id != actorUserId && readableIds.Contains(user.Id)) users.Add(user);
        }
        return users;
    }

    private static bool TryNormalizeText(string? value, out string content)
    {
        content = string.Empty;
        if (value is null)
        {
            return false;
        }

        content = value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Trim();
        return content.Length > 0 && content.EnumerateRunes().Count() <= 10_000;
    }

    private static string ComputePayloadHash(MessageType messageType, string content, Guid? replyToMessageId, Guid? threadRootId)
    {
        var canonical = replyToMessageId is null && threadRootId is null
            ? $"{(short)messageType}:{content}"
            : $"{(short)messageType}:{content.Length}:{content}:{replyToMessageId:N}:{threadRootId:N}";
        var bytes = Encoding.UTF8.GetBytes(canonical);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private async Task<IReadOnlyList<MessageDto>> ToDtosAsync(
        IReadOnlyCollection<Message> messages,
        CancellationToken cancellationToken)
    {
        var authorIds = messages
            .Where(message => message.AuthorUserId is not null)
            .Select(message => message.AuthorUserId!.Value)
            .Distinct()
            .ToArray();
        var authors = await userDirectory.FindByIdsAsync(authorIds, cancellationToken);
        var messageIds = messages.Select(message => message.Id).ToArray();
        var mentions = await dbContext.MessageMentions.AsNoTracking()
            .Where(mention => messageIds.Contains(mention.MessageId))
            .ToListAsync(cancellationToken);
        var mentionedUsers = await userDirectory.FindByIdsAsync(
            mentions.Select(mention => mention.MentionedUserId).Distinct().ToArray(), cancellationToken);
        var mentionsByMessage = mentions.Where(mention => mentionedUsers.ContainsKey(mention.MentionedUserId))
            .GroupBy(mention => mention.MessageId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<MentionSummaryDto>)group
                .Select(mention => new MentionSummaryDto(mention.MentionedUserId,
                    mentionedUsers[mention.MentionedUserId].Username)).ToArray());
        var rootIds = messages.Where(message => message.ThreadRootId is null)
            .Select(message => message.Id).ToArray();
        var counts = rootIds.Length == 0
            ? new Dictionary<Guid, int>()
            : await dbContext.Messages.AsNoTracking()
                .Where(message => message.ThreadRootId != null && rootIds.Contains(message.ThreadRootId.Value)
                                  && message.DeletedAt == null)
                .GroupBy(message => message.ThreadRootId!.Value)
                .Select(group => new { RootId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(row => row.RootId, row => row.Count, cancellationToken);
        return messages.Select(message => ToDto(
            message,
            message.AuthorUserId is { } authorId && authors.TryGetValue(authorId, out var author) ? author : null,
            counts.GetValueOrDefault(message.Id),
            message.DeletedAt is null ? mentionsByMessage.GetValueOrDefault(message.Id) ?? [] : []))
            .ToArray();
    }

    private static bool TryParseCursor(string? value, bool allowZero, out long? sequence)
    {
        sequence = null;
        if (value is null)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(value)
            || (value.Length > 1 && value[0] == '0')
            || !long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
            || parsed < 0
            || (!allowZero && parsed == 0))
        {
            return false;
        }

        sequence = parsed;
        return true;
    }

    private static string ToSequence(long sequence) => sequence.ToString(CultureInfo.InvariantCulture);

    private static MessageDto ToDto(Message message, UserSummary? author, int threadCount = 0,
        IReadOnlyList<MentionSummaryDto>? mentions = null) => new(
        message.Id,
        message.SpaceId,
        message.ClientMessageId,
        message.SequenceNo.ToString(CultureInfo.InvariantCulture),
        (short)message.MessageType,
        author,
        message.DeletedAt is null ? message.Content : null,
        message.Version,
        message.CreatedAt,
        message.EditedAt,
        message.DeletedAt,
        ReplyToMessageId: message.ReplyToMessageId,
        ThreadRootId: message.ThreadRootId,
        Attachments: [],
        Reactions: [],
        IsPinned: false,
        ThreadCount: threadCount,
        Mentions: mentions ?? []);
}
