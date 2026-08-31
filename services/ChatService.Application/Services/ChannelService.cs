using ChatService.Common.Messaging;
using ChatService.Domain.Entities;
using ChatService.Dtos;
using ChatService.Services.Abstractions;

namespace ChatService.Services;

public sealed class ChannelService(
    IChannelRepository channels,
    IServerRepository servers,
    IMessageRepository messages,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    ICurrentUserContext currentUser,
    TimeProvider timeProvider) : IChannelService
{
    public async Task<ServiceResult<ItemsResponse<ChannelResponse>>> ListAsync(
        Guid serverId,
        CancellationToken cancellationToken)
    {
        if (!await servers.IsMemberAsync(serverId, currentUser.UserId, cancellationToken))
        {
            return ServiceResult<ItemsResponse<ChannelResponse>>.NotFound("Server not found.");
        }

        var entities = await channels.ListByServerAsync(serverId, cancellationToken);
        var items = entities
            .Select(channel => new ChannelResponse(
                channel.Id,
                channel.ServerId,
                channel.Name,
                channel.CreatedAt))
            .ToList();

        return ServiceResult<ItemsResponse<ChannelResponse>>.Ok(
            new ItemsResponse<ChannelResponse>(items));
    }

    public async Task<ServiceResult<ChannelResponse>> CreateAsync(
        Guid serverId,
        CreateChannelRequest request,
        CancellationToken cancellationToken)
    {
        if (!await servers.IsOwnerAsync(serverId, currentUser.UserId, cancellationToken))
        {
            return ServiceResult<ChannelResponse>.NotFound(
                "Server not found or the current user is not its owner.");
        }

        var now = timeProvider.GetUtcNow();
        var channel = new ChatChannel
        {
            ServerId = serverId,
            Name = request.Name.Trim(),
            CreatedAt = now
        };
        channels.Add(channel);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateKeyException)
        {
            return ServiceResult<ChannelResponse>.Conflict("Channel name already exists.");
        }

        return ServiceResult<ChannelResponse>.Created(
            new ChannelResponse(channel.Id, channel.ServerId, channel.Name, channel.CreatedAt));
    }

    public async Task<ServiceResult<MessageHistoryResponse>> GetMessagesAsync(
        Guid channelId,
        string? before,
        int limit,
        CancellationToken cancellationToken)
    {
        if (limit is < 1 or > 100)
        {
            return ServiceResult<MessageHistoryResponse>.Validation(
                "limit",
                "Limit must be between 1 and 100.");
        }

        DecodedMessageCursor? cursor = null;
        if (before is not null)
        {
            if (!MessageCursor.TryDecode(before, out var decoded))
            {
                return ServiceResult<MessageHistoryResponse>.Validation(
                    "before",
                    "The message cursor is invalid.");
            }

            cursor = decoded;
        }

        if (!await channels.CanAccessAsync(channelId, currentUser.UserId, cancellationToken))
        {
            return ServiceResult<MessageHistoryResponse>.NotFound("Channel not found.");
        }

        var rows = await messages.GetHistoryAsync(
            channelId,
            cursor?.CreatedAt,
            cursor?.MessageId,
            limit + 1,
            cancellationToken);
        var hasMore = rows.Count > limit;
        var page = rows.Take(limit).ToList();
        var items = page.Select(row => DtoMappings.ToMessage(row.Message, row.Author)).ToList();
        var last = page.LastOrDefault();
        var nextCursor = hasMore && last is not null
            ? MessageCursor.Encode(last.Message.CreatedAt, last.Message.Id)
            : null;

        return ServiceResult<MessageHistoryResponse>.Ok(
            new MessageHistoryResponse(items, nextCursor, hasMore));
    }

    public async Task<ServiceResult<MessageResponse>> SendMessageAsync(
        Guid channelId,
        SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ClientMessageId == Guid.Empty)
        {
            return ServiceResult<MessageResponse>.Validation(
                "clientMessageId",
                "Client message ID is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return ServiceResult<MessageResponse>.Validation(
                "content",
                "Message content cannot contain only whitespace.");
        }

        var userId = currentUser.UserId;
        if (!await channels.CanAccessAsync(channelId, userId, cancellationToken))
        {
            return ServiceResult<MessageResponse>.NotFound("Channel not found.");
        }

        var author = await users.FindByIdAsync(userId, cancellationToken, tracking: false)
            ?? throw new InvalidOperationException("The authenticated user no longer exists.");
        var existing = await messages.FindByClientMessageIdAsync(
            userId,
            request.ClientMessageId,
            cancellationToken);
        if (existing is not null)
        {
            return ExistingMessageResult(existing, author, channelId);
        }

        var now = timeProvider.GetUtcNow();
        var message = new ChatMessage
        {
            ChannelId = channelId,
            AuthorUserId = userId,
            ClientMessageId = request.ClientMessageId,
            Content = request.Content,
            CreatedAt = now
        };
        var response = DtoMappings.ToMessage(message, author);
        var outboxEvent = OutboxFactory.Create(
            ChatEventTypes.MessageCreated,
            message.Id,
            message.Version,
            channelId,
            response,
            now);

        if (await messages.TryAddWithOutboxAsync(message, outboxEvent, cancellationToken))
        {
            return ServiceResult<MessageResponse>.Created(response);
        }

        var concurrentExisting = await messages.FindByClientMessageIdAsync(
            userId,
            request.ClientMessageId,
            cancellationToken)
            ?? throw new InvalidOperationException("The conflicting message could not be loaded.");
        return ExistingMessageResult(concurrentExisting, author, channelId);
    }

    private static ServiceResult<MessageResponse> ExistingMessageResult(
        ChatMessage existing,
        User author,
        Guid requestedChannelId) =>
        existing.DeletedAt is not null || existing.ChannelId != requestedChannelId
            ? ServiceResult<MessageResponse>.Conflict("Client message ID has already been used.")
            : ServiceResult<MessageResponse>.Ok(DtoMappings.ToMessage(existing, author));
}
