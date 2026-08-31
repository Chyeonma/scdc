using ChatService.Data;
using ChatService.Domain.Entities;
using ChatService.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Infrastructure.Persistence;

public sealed class ChannelRepository(ChatDbContext dbContext) : IChannelRepository
{
    public async Task<IReadOnlyList<ChatChannel>> ListByServerAsync(
        Guid serverId,
        CancellationToken cancellationToken) =>
        await dbContext.Channels.AsNoTracking()
            .Where(channel => channel.ServerId == serverId)
            .OrderBy(channel => channel.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> ListIdsByServerAsync(
        Guid serverId,
        CancellationToken cancellationToken) =>
        await dbContext.Channels.AsNoTracking()
            .Where(channel => channel.ServerId == serverId)
            .Select(channel => channel.Id)
            .ToListAsync(cancellationToken);

    public Task<bool> CanAccessAsync(
        Guid channelId,
        Guid userId,
        CancellationToken cancellationToken) =>
        (
            from channel in dbContext.Channels
            join membership in dbContext.ServerMembers on channel.ServerId equals membership.ServerId
            where channel.Id == channelId && membership.UserId == userId
            select channel.Id)
        .AnyAsync(cancellationToken);

    public void Add(ChatChannel channel) => dbContext.Channels.Add(channel);
}
