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
}
