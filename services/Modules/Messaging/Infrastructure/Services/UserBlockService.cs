using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class UserBlockService(
    MessagingDbContext db,
    IUserDirectory users,
    MessagingRealtimeAccessRevoker realtime,
    TimeProvider timeProvider,
    ILogger<UserBlockService> logger) : IUserBlockService
{
    public async Task<Result<IReadOnlyList<UserBlockDto>>> ListAsync(Guid actorUserId, CancellationToken ct)
    {
        if (await users.FindByIdAsync(actorUserId, ct) is null)
            return Result.Failure<IReadOnlyList<UserBlockDto>>(MessagingErrors.AccountUnavailable);

        var rows = await db.UserBlocks.AsNoTracking()
            .Where(block => block.BlockerUserId == actorUserId)
            .OrderByDescending(block => block.CreatedAt)
            .ThenBy(block => block.BlockedUserId)
            .ToListAsync(ct);
        var summaries = await users.FindByIdsAsync(rows.Select(row => row.BlockedUserId).ToArray(), ct);
        return Result.Success<IReadOnlyList<UserBlockDto>>(rows.Select(row =>
        {
            summaries.TryGetValue(row.BlockedUserId, out var summary);
            return new UserBlockDto(row.BlockedUserId, summary?.Username, summary?.DisplayName, row.CreatedAt);
        }).ToArray());
    }

    public async Task<Result> BlockAsync(Guid actorUserId, Guid targetUserId, CancellationToken ct)
    {
        var validation = await ValidateAsync(actorUserId, targetUserId, ct);
        if (validation.IsFailure) return validation;

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await UserBlockPairLock.LockAsync(db, actorUserId, targetUserId, ct);
        var alreadyBlocked = await db.UserBlocks.AsNoTracking().AnyAsync(block =>
            block.BlockerUserId == actorUserId && block.BlockedUserId == targetUserId, ct);
        if (!alreadyBlocked && await users.FindByIdAsync(targetUserId, ct) is null)
            return Result.Failure(MessagingErrors.ResourceNotFound);

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO messaging.user_blocks (blocker_user_id, blocked_user_id, created_at)
            VALUES ({actorUserId}, {targetUserId}, {timeProvider.GetUtcNow()})
            ON CONFLICT (blocker_user_id, blocked_user_id) DO NOTHING
            """, ct);
        await transaction.CommitAsync(ct);
        await NotifyDirectBestEffortAsync(actorUserId, targetUserId, ct);
        return Result.Success();
    }

    public async Task<Result> UnblockAsync(Guid actorUserId, Guid targetUserId, CancellationToken ct)
    {
        var validation = await ValidateAsync(actorUserId, targetUserId, ct);
        if (validation.IsFailure) return validation;

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await UserBlockPairLock.LockAsync(db, actorUserId, targetUserId, ct);
        await db.UserBlocks.Where(block => block.BlockerUserId == actorUserId
            && block.BlockedUserId == targetUserId).ExecuteDeleteAsync(ct);
        await transaction.CommitAsync(ct);
        await NotifyDirectBestEffortAsync(actorUserId, targetUserId, ct);
        return Result.Success();
    }

    private async Task<Result> ValidateAsync(Guid actorUserId, Guid targetUserId, CancellationToken ct)
    {
        if (targetUserId == Guid.Empty) return Result.Failure(MessagingErrors.InvalidBlockTarget);
        if (actorUserId == targetUserId) return Result.Failure(MessagingErrors.SelfBlockNotAllowed);
        if (await users.FindByIdAsync(actorUserId, ct) is null)
            return Result.Failure(MessagingErrors.AccountUnavailable);
        return Result.Success();
    }

    private async Task NotifyDirectBestEffortAsync(Guid actorUserId, Guid targetUserId, CancellationToken ct)
    {
        try
        {
            var spaceId = await db.DirectConversations.AsNoTracking()
                .Where(direct => direct.UserLowId == actorUserId && direct.UserHighId == targetUserId
                    || direct.UserLowId == targetUserId && direct.UserHighId == actorUserId)
                .Select(direct => (Guid?)direct.SpaceId).SingleOrDefaultAsync(ct);
            if (spaceId is { } id)
                await realtime.NotifySpaceUpdatedAsync([actorUserId, targetUserId], id, ct);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not notify users after changing a user block.");
        }
    }
}
