using System.Data.Common;

namespace SCDC.Contracts.Messaging;

public interface IChatSpaceLifecycle
{
    // Caller owns the transaction; space and Community metadata must commit together.
    Task CreateChannelAsync(Guid channelId, Guid actorId, DateTimeOffset createdAt,
        DbTransaction transaction, CancellationToken cancellationToken);
}
