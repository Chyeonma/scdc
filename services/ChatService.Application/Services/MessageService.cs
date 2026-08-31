using ChatService.Common.Messaging;
using ChatService.Dtos;
using ChatService.Services.Abstractions;

namespace ChatService.Services;

public sealed class MessageService(
    IMessageRepository messages,
    IOutboxRepository outbox,
    IUnitOfWork unitOfWork,
    ICurrentUserContext currentUser,
    TimeProvider timeProvider) : IMessageService
{
    public async Task<ServiceResult<MessageResponse>> UpdateAsync(
        Guid messageId,
        UpdateMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return ServiceResult<MessageResponse>.Validation(
                "content",
                "Message content cannot contain only whitespace.");
        }

        var row = await messages.FindWithAuthorAsync(messageId, cancellationToken);
        if (row is null || row.Message.DeletedAt is not null)
        {
            return ServiceResult<MessageResponse>.NotFound("Message not found.");
        }

        if (row.Message.AuthorUserId != currentUser.UserId)
        {
            return ServiceResult<MessageResponse>.Forbidden(
                "Only the message author can perform this operation.");
        }

        var now = timeProvider.GetUtcNow();
        row.Message.Content = request.Content;
        row.Message.EditedAt = now;
        row.Message.Version++;
        var response = DtoMappings.ToMessage(row.Message, row.Author);
        outbox.Add(OutboxFactory.Create(
            ChatEventTypes.MessageUpdated,
            row.Message.Id,
            row.Message.Version,
            row.Message.ChannelId,
            response,
            now));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<MessageResponse>.Ok(response);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var message = await messages.FindByIdAsync(messageId, cancellationToken);
        if (message is null)
        {
            return ServiceResult<bool>.NotFound("Message not found.");
        }

        if (message.AuthorUserId != currentUser.UserId)
        {
            return ServiceResult<bool>.Forbidden(
                "Only the message author can perform this operation.");
        }

        if (message.DeletedAt is not null)
        {
            return ServiceResult<bool>.NoContent();
        }

        var now = timeProvider.GetUtcNow();
        message.DeletedAt = now;
        message.Version++;
        var payload = new
        {
            messageId = message.Id,
            channelId = message.ChannelId,
            deletedAt = now,
            version = message.Version
        };
        outbox.Add(OutboxFactory.Create(
            ChatEventTypes.MessageDeleted,
            message.Id,
            message.Version,
            message.ChannelId,
            payload,
            now));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.NoContent();
    }
}
