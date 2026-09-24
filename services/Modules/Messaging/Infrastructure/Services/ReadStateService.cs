using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Domain;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class ReadStateService(
    MessagingDbContext dbContext,
    IUserDirectory userDirectory,
    SpaceMessageAccess spaceAccess,
    MessagingRealtimeAccessRevoker realtimeEvents,
    TimeProvider timeProvider,
    ILogger<ReadStateService> logger) : IReadStateService
{
    public async Task<Result<ReadStateDto>> UpdateAsync(
        Guid actorUserId,
        Guid spaceId,
        string? lastReadSequence,
        CancellationToken cancellationToken)
    {
        if (actorUserId == Guid.Empty || await userDirectory.FindByIdAsync(actorUserId, cancellationToken) is null)
            return Result.Failure<ReadStateDto>(MessagingErrors.AccountUnavailable);
        if (spaceId == Guid.Empty)
            return Result.Failure<ReadStateDto>(MessagingErrors.ResourceNotFound);
        if (lastReadSequence is null
            || lastReadSequence.Length == 0
            || lastReadSequence[0] == '0'
            || lastReadSequence.Any(character => character is < '0' or > '9')
            || !long.TryParse(lastReadSequence, NumberStyles.None, CultureInfo.InvariantCulture, out var sequence))
            return Result.Failure<ReadStateDto>(MessagingErrors.InvalidReadState);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var space = await dbContext.Spaces
            .FromSqlInterpolated($"SELECT * FROM messaging.spaces WHERE id = {spaceId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (space is null || space.Status == SpaceStatus.Deleted
            || !(await spaceAccess.CheckAsync(actorUserId, space, cancellationToken)).CanRead)
            return Result.Failure<ReadStateDto>(MessagingErrors.ResourceNotFound);

        if (!await dbContext.Messages.AsNoTracking().AnyAsync(
                message => message.SpaceId == spaceId && message.SequenceNo == sequence,
                cancellationToken))
            return Result.Failure<ReadStateDto>(MessagingErrors.InvalidReadState);

        var now = timeProvider.GetUtcNow();
        var changed = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO messaging.space_user_states (space_id, user_id, last_read_sequence, last_read_at)
            VALUES ({spaceId}, {actorUserId}, {sequence}, {now})
            ON CONFLICT (space_id, user_id) DO UPDATE
            SET last_read_sequence = EXCLUDED.last_read_sequence,
                last_read_at = EXCLUDED.last_read_at
            WHERE messaging.space_user_states.last_read_sequence IS NULL
               OR messaging.space_user_states.last_read_sequence < EXCLUDED.last_read_sequence
            """, cancellationToken);
        var state = await dbContext.SpaceUserStates.AsNoTracking().SingleAsync(
            item => item.SpaceId == spaceId && item.UserId == actorUserId,
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        if (changed > 0)
        {
            try
            {
                await realtimeEvents.NotifySpaceUpdatedAsync([actorUserId], spaceId, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // The committed position remains authoritative; clients refetch on reconnect.
                logger.LogWarning(exception, "Could not publish read state for space {SpaceId}", spaceId);
            }
        }

        return Result.Success(new ReadStateDto(
            spaceId,
            state.LastReadSequence!.Value.ToString(CultureInfo.InvariantCulture),
            state.LastReadAt!.Value));
    }
}
