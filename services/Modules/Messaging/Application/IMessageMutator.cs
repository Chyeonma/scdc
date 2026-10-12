using SCDC.BuildingBlocks.Application.Results;
namespace SCDC.Modules.Messaging.Application;

public sealed record MutateMessage(Guid ActorId, Guid SessionId, Guid SecurityStamp,
    Guid ConversationId, Guid MessageId, string? ExpectedVersion, string? Content, bool Delete);
public interface IMessageMutator
{
    Task<Result<MessageResponse>> MutateAsync(MutateMessage command, CancellationToken cancellationToken);
}
