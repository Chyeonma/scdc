using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    MessageRateLimiter rateLimiter,
    TimeProvider timeProvider) : IMessageService
{
    public async Task<Result<SendMessageResult>> SendAsync(
        SendMessageCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ActorUserId == Guid.Empty || command.SpaceId == Guid.Empty || command.ClientMessageId == Guid.Empty)
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

        var payloadHash = ComputePayloadHash(MessageType.Text, content);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var space = await dbContext.Spaces
            .FromSqlInterpolated($"SELECT * FROM messaging.spaces WHERE id = {command.SpaceId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (space is null || space.Status == SpaceStatus.Deleted)
        {
            return Result.Failure<SendMessageResult>(MessagingErrors.ResourceNotFound);
        }

        var isActiveMember = await dbContext.SpaceMembers
            .AsNoTracking()
            .AnyAsync(member => member.SpaceId == command.SpaceId
                                && member.UserId == command.ActorUserId
                                && member.MembershipStatus == SpaceMembershipStatus.Active,
                cancellationToken);
        if (!isActiveMember)
        {
            return Result.Failure<SendMessageResult>(MessagingErrors.ResourceNotFound);
        }

        if (space.Status != SpaceStatus.Active)
        {
            return Result.Failure<SendMessageResult>(MessagingErrors.SpaceNotWritable);
        }

        if (space.SpaceType != SpaceType.Direct)
        {
            return Result.Failure<SendMessageResult>(MessagingErrors.ActionNotAllowed);
        }

        var directConversation = await dbContext.DirectConversations
            .AsNoTracking()
            .SingleOrDefaultAsync(conversation => conversation.SpaceId == command.SpaceId, cancellationToken);
        if (directConversation is null)
        {
            return Result.Failure<SendMessageResult>(MessagingErrors.ResourceNotFound);
        }

        var peerUserId = directConversation.UserLowId == command.ActorUserId
            ? directConversation.UserHighId
            : directConversation.UserLowId;
        if (await userDirectory.FindByIdAsync(peerUserId, cancellationToken) is null
            || await IsBlockedAsync(command.ActorUserId, peerUserId, cancellationToken))
        {
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
                ? Result.Success(new SendMessageResult(ToDto(existing, actor), Created: false))
                : Result.Failure<SendMessageResult>(MessagingErrors.IdempotencyConflict);
        }

        var now = timeProvider.GetUtcNow();
        var message = new Message
        {
            Id = Guid.CreateVersion7(),
            SpaceId = command.SpaceId,
            AuthorUserId = command.ActorUserId,
            ClientMessageId = command.ClientMessageId,
            MessageType = MessageType.Text,
            Content = content,
            IdempotencyPayloadHash = payloadHash,
            Version = 1,
            CreatedAt = now
        };
        dbContext.Messages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);

        space.LastMessageId = message.Id;
        space.LastMessageSequence = message.SequenceNo;
        space.LastActivityAt = now;
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

        return Result.Success(new SendMessageResult(ToDto(message, actor), Created: true));
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

        var canRead = await (
            from space in dbContext.Spaces.AsNoTracking()
            join member in dbContext.SpaceMembers.AsNoTracking() on space.Id equals member.SpaceId
            where space.Id == query.SpaceId
                  && space.Status != SpaceStatus.Deleted
                  && space.SpaceType == SpaceType.Direct
                  && member.UserId == query.ActorUserId
                  && member.MembershipStatus == SpaceMembershipStatus.Active
            select space.Id)
            .AnyAsync(cancellationToken);
        if (!canRead)
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

    private async Task<bool> IsBlockedAsync(Guid actorUserId, Guid peerUserId, CancellationToken cancellationToken) =>
        await dbContext.UserBlocks
            .AsNoTracking()
            .AnyAsync(block => (block.BlockerUserId == actorUserId && block.BlockedUserId == peerUserId)
                               || (block.BlockerUserId == peerUserId && block.BlockedUserId == actorUserId),
                cancellationToken);

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

    private static string ComputePayloadHash(MessageType messageType, string content)
    {
        var bytes = Encoding.UTF8.GetBytes($"{(short)messageType}:{content}");
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
        return messages.Select(message => ToDto(
            message,
            message.AuthorUserId is { } authorId && authors.TryGetValue(authorId, out var author) ? author : null))
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

    private static MessageDto ToDto(Message message, UserSummary? author) => new(
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
        ReplyToMessageId: null,
        ThreadRootId: null,
        Attachments: [],
        Reactions: [],
        IsPinned: false,
        ThreadCount: 0);
}
