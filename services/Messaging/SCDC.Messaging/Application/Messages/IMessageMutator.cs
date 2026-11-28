using SCDC.BuildingBlocks.Application.Results;
using SCDC.Modules.Messaging.Domain;
namespace SCDC.Modules.Messaging.Application;

public sealed record MutateMessage(Guid ActorId, Guid SessionId, Guid SecurityStamp,
    Guid ConversationId, Guid MessageId, string? ExpectedVersion, string? Content, bool Delete, DateTimeOffset AccessExpiresAt = default,
    ConversationKind Kind = ConversationKind.Direct);
public interface IMessageMutator
{
    Task<Result<MessageResponse>> MutateAsync(MutateMessage command, CancellationToken cancellationToken);
}
