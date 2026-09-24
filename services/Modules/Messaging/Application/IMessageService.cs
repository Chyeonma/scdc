using SCDC.BuildingBlocks.Application.Results;

namespace SCDC.Modules.Messaging.Application;

public interface IMessageService
{
    Task<Result<SendMessageResult>> SendAsync(
        SendMessageCommand command,
        CancellationToken cancellationToken);

    Task<Result<MessagePageDto>> GetHistoryAsync(
        GetMessagesQuery query,
        CancellationToken cancellationToken);

    Task<Result<MessageDto>> GetAsync(Guid actorUserId, Guid spaceId, Guid messageId, CancellationToken cancellationToken);

    Task<Result<MessageDto>> EditAsync(EditMessageCommand command, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(DeleteMessageCommand command, CancellationToken cancellationToken);
}
