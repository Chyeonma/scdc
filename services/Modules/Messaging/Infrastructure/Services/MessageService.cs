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

    private static MessageDto ToDto(Message message, UserSummary author) => new(
        message.Id,
        message.SpaceId,
        message.ClientMessageId,
        message.SequenceNo.ToString(CultureInfo.InvariantCulture),
        (short)message.MessageType,
        author,
        message.Content,
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
