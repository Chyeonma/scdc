using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SCDC.Contracts.Messaging;
using SCDC.Modules.Messaging.Domain;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class UnreadCountReader(MessagingDbContext dbContext, TimeProvider timeProvider) : IUnreadCountReader
{
    public async Task<IReadOnlyDictionary<Guid, SpaceUnreadState>> GetAsync(
        Guid userId,
        IReadOnlyCollection<Guid> spaceIds,
        CancellationToken cancellationToken)
    {
        var ids = spaceIds.Where(id => id != Guid.Empty).Distinct().ToArray();
        if (userId == Guid.Empty || ids.Length == 0)
            return new Dictionary<Guid, SpaceUnreadState>();

        var states = await dbContext.SpaceUserStates.AsNoTracking()
            .Where(state => state.UserId == userId && ids.Contains(state.SpaceId))
            .ToDictionaryAsync(state => state.SpaceId, cancellationToken);

        // Count actual eligible rows. Sequence numbers are global and can have gaps.
        var counts = await (
            from message in dbContext.Messages.AsNoTracking()
            join state in dbContext.SpaceUserStates.AsNoTracking().Where(item => item.UserId == userId)
                on message.SpaceId equals state.SpaceId into stateRows
            from state in stateRows.DefaultIfEmpty()
            where ids.Contains(message.SpaceId)
                  && message.SequenceNo > (state == null ? 0 : state.LastReadSequence ?? 0)
                  && message.AuthorUserId != userId
                  && message.MessageType != MessageType.System
                  && message.DeletedAt == null
                  && message.ThreadRootId == null
            group message by message.SpaceId into grouped
            select new { SpaceId = grouped.Key, Count = grouped.Count() })
            .ToDictionaryAsync(row => row.SpaceId, row => row.Count, cancellationToken);

        var now = timeProvider.GetUtcNow();
        return ids.ToDictionary(id => id, id =>
        {
            var state = states.GetValueOrDefault(id);
            var unreadCount = counts.GetValueOrDefault(id);
            var level = state?.NotificationLevel ?? NotificationLevel.AllMessages;
            var muted = state?.MutedUntil is { } until && until > now;
            return new SpaceUnreadState(
                unreadCount,
                level == NotificationLevel.AllMessages && !muted ? unreadCount : 0,
                state?.LastReadSequence?.ToString(CultureInfo.InvariantCulture),
                new UserSpacePreferencesDto((short)level, state?.MutedUntil, state?.IsHidden ?? false, state?.IsPinned ?? false));
        });
    }
}
