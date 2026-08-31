using ChatService.Domain.Entities;

namespace ChatService.Services.Abstractions;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}

public interface IAppTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}

public sealed class DuplicateKeyException(string message, Exception innerException)
    : Exception(message, innerException);

public interface IUserRepository
{
    Task<bool> ExistsAsync(
        string normalizedEmail,
        string normalizedUsername,
        CancellationToken cancellationToken);
    Task<User?> FindByLoginAsync(string normalizedLogin, CancellationToken cancellationToken);
    Task<User?> FindByIdAsync(Guid userId, CancellationToken cancellationToken, bool tracking = true);
    Task<User?> FindByNormalizedUsernameAsync(string normalizedUsername, CancellationToken cancellationToken);
    void Add(User user);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task<RefreshToken?> FindByHashForUpdateAsync(string tokenHash, CancellationToken cancellationToken);
    Task<IReadOnlyList<RefreshToken>> ListActiveFamilyAsync(Guid familyId, CancellationToken cancellationToken);
    void Add(RefreshToken token);
}

public interface IServerRepository
{
    Task<IReadOnlyList<ChatServer>> ListForUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<ChatServer?> FindForMemberAsync(Guid serverId, Guid userId, CancellationToken cancellationToken);
    Task<ChatServer?> FindByIdAsync(Guid serverId, CancellationToken cancellationToken);
    Task<bool> IsOwnerAsync(Guid serverId, Guid userId, CancellationToken cancellationToken);
    Task<bool> IsMemberAsync(Guid serverId, Guid userId, CancellationToken cancellationToken);
    Task<ServerMember?> FindMembershipAsync(Guid serverId, Guid userId, CancellationToken cancellationToken);
    void AddAggregate(ChatServer server, ServerMember ownerMembership, ChatChannel defaultChannel);
    void AddMember(ServerMember membership);
    void RemoveMember(ServerMember membership);
}

public interface IChannelRepository
{
    Task<IReadOnlyList<ChatChannel>> ListByServerAsync(Guid serverId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> ListIdsByServerAsync(Guid serverId, CancellationToken cancellationToken);
    Task<bool> CanAccessAsync(Guid channelId, Guid userId, CancellationToken cancellationToken);
    void Add(ChatChannel channel);
}

public interface IMessageRepository
{
    Task<ChatMessage?> FindByClientMessageIdAsync(
        Guid authorUserId,
        Guid clientMessageId,
        CancellationToken cancellationToken);
    Task<MessageWithAuthor?> FindWithAuthorAsync(Guid messageId, CancellationToken cancellationToken);
    Task<ChatMessage?> FindByIdAsync(Guid messageId, CancellationToken cancellationToken);
    Task<IReadOnlyList<MessageWithAuthor>> GetHistoryAsync(
        Guid channelId,
        DateTimeOffset? beforeCreatedAt,
        Guid? beforeMessageId,
        int take,
        CancellationToken cancellationToken);
    Task<bool> TryAddWithOutboxAsync(
        ChatMessage message,
        OutboxEvent outboxEvent,
        CancellationToken cancellationToken);
}

public sealed record MessageWithAuthor(ChatMessage Message, User Author);

public interface IOutboxRepository
{
    void Add(OutboxEvent outboxEvent);
}
