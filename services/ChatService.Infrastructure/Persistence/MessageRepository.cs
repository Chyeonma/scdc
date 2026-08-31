using ChatService.Data;
using ChatService.Domain.Entities;
using ChatService.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ChatService.Infrastructure.Persistence;

public sealed class MessageRepository(ChatDbContext dbContext) : IMessageRepository
{
    public Task<ChatMessage?> FindByClientMessageIdAsync(
        Guid authorUserId,
        Guid clientMessageId,
        CancellationToken cancellationToken) =>
        dbContext.Messages.AsNoTracking().SingleOrDefaultAsync(
            message => message.AuthorUserId == authorUserId && message.ClientMessageId == clientMessageId,
            cancellationToken);

    public Task<MessageWithAuthor?> FindWithAuthorAsync(
        Guid messageId,
        CancellationToken cancellationToken) =>
        (
            from message in dbContext.Messages
            join author in dbContext.Users.AsNoTracking() on message.AuthorUserId equals author.Id
            where message.Id == messageId
            select new MessageWithAuthor(message, author))
        .SingleOrDefaultAsync(cancellationToken);

    public Task<ChatMessage?> FindByIdAsync(Guid messageId, CancellationToken cancellationToken) =>
        dbContext.Messages.SingleOrDefaultAsync(message => message.Id == messageId, cancellationToken);

    public async Task<IReadOnlyList<MessageWithAuthor>> GetHistoryAsync(
        Guid channelId,
        DateTimeOffset? beforeCreatedAt,
        Guid? beforeMessageId,
        int take,
        CancellationToken cancellationToken)
    {
        var messages = dbContext.Messages.AsNoTracking()
            .Where(message => message.ChannelId == channelId && message.DeletedAt == null);
        if (beforeCreatedAt is not null && beforeMessageId is not null)
        {
            var cursorCreatedAt = beforeCreatedAt.Value;
            var cursorMessageId = beforeMessageId.Value;
            messages = messages.Where(message =>
                message.CreatedAt < cursorCreatedAt ||
                (message.CreatedAt == cursorCreatedAt && message.Id.CompareTo(cursorMessageId) < 0));
        }

        return await (
            from message in messages
            join author in dbContext.Users.AsNoTracking() on message.AuthorUserId equals author.Id
            orderby message.CreatedAt descending, message.Id descending
            select new MessageWithAuthor(message, author))
        .Take(take)
        .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryAddWithOutboxAsync(
        ChatMessage message,
        OutboxEvent outboxEvent,
        CancellationToken cancellationToken)
    {
        dbContext.AddRange(message, outboxEvent);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            dbContext.Entry(message).State = EntityState.Detached;
            dbContext.Entry(outboxEvent).State = EntityState.Detached;
            return false;
        }
    }
}
