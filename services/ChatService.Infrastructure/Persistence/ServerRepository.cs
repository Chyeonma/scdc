using ChatService.Data;
using ChatService.Domain.Entities;
using ChatService.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Infrastructure.Persistence;

public sealed class ServerRepository(ChatDbContext dbContext) : IServerRepository
{
    public async Task<IReadOnlyList<ChatServer>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await (
            from membership in dbContext.ServerMembers.AsNoTracking()
            join server in dbContext.Servers.AsNoTracking() on membership.ServerId equals server.Id
            where membership.UserId == userId
            orderby server.CreatedAt
            select server)
        .ToListAsync(cancellationToken);

    public Task<ChatServer?> FindForMemberAsync(
        Guid serverId,
        Guid userId,
        CancellationToken cancellationToken) =>
        (
            from server in dbContext.Servers.AsNoTracking()
            join membership in dbContext.ServerMembers.AsNoTracking() on server.Id equals membership.ServerId
            where server.Id == serverId && membership.UserId == userId
            select server)
        .SingleOrDefaultAsync(cancellationToken);

    public Task<ChatServer?> FindByIdAsync(Guid serverId, CancellationToken cancellationToken) =>
        dbContext.Servers.AsNoTracking().SingleOrDefaultAsync(
            server => server.Id == serverId,
            cancellationToken);

    public Task<bool> IsOwnerAsync(Guid serverId, Guid userId, CancellationToken cancellationToken) =>
        dbContext.Servers.AnyAsync(
            server => server.Id == serverId && server.OwnerUserId == userId,
            cancellationToken);

    public Task<bool> IsMemberAsync(Guid serverId, Guid userId, CancellationToken cancellationToken) =>
        dbContext.ServerMembers.AnyAsync(
            membership => membership.ServerId == serverId && membership.UserId == userId,
            cancellationToken);

    public Task<ServerMember?> FindMembershipAsync(
        Guid serverId,
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.ServerMembers.SingleOrDefaultAsync(
            membership => membership.ServerId == serverId && membership.UserId == userId,
            cancellationToken);

    public void AddAggregate(
        ChatServer server,
        ServerMember ownerMembership,
        ChatChannel defaultChannel) =>
        dbContext.AddRange(server, ownerMembership, defaultChannel);

    public void AddMember(ServerMember membership) => dbContext.ServerMembers.Add(membership);
    public void RemoveMember(ServerMember membership) => dbContext.ServerMembers.Remove(membership);
}
