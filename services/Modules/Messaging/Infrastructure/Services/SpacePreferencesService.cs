using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;
using SCDC.Contracts.Messaging;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Domain;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class SpacePreferencesService(
    MessagingDbContext dbContext,
    IUserDirectory userDirectory,
    SpaceMessageAccess spaceAccess,
    MessagingRealtimeAccessRevoker realtimeEvents,
    TimeProvider timeProvider,
    ILogger<SpacePreferencesService> logger) : ISpacePreferencesService
{
    public async Task<Result<UserSpacePreferencesDto>> GetAsync(Guid actorUserId, Guid spaceId, CancellationToken cancellationToken)
    {
        var access = await CheckAccessAsync(actorUserId, spaceId, cancellationToken);
        if (access.IsFailure) return Result.Failure<UserSpacePreferencesDto>(access.Error);
        var state = await dbContext.SpaceUserStates.AsNoTracking().SingleOrDefaultAsync(
            item => item.SpaceId == spaceId && item.UserId == actorUserId, cancellationToken);
        return Result.Success(ToDto(state));
    }

    public async Task<Result<UserSpacePreferencesDto>> UpdateAsync(
        Guid actorUserId,
        Guid spaceId,
        UserSpacePreferencesDto? preferences,
        CancellationToken cancellationToken)
    {
        if (preferences is null)
            return Result.Failure<UserSpacePreferencesDto>(MessagingErrors.InvalidPreferences);
        preferences = preferences with { MutedUntil = preferences.MutedUntil?.ToUniversalTime() };
        if (preferences.NotificationLevel is < 0 or > 2
            || preferences.MutedUntil is { } until && until <= timeProvider.GetUtcNow())
            return Result.Failure<UserSpacePreferencesDto>(MessagingErrors.InvalidPreferences);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var access = await CheckAccessAsync(actorUserId, spaceId, cancellationToken);
        if (access.IsFailure) return Result.Failure<UserSpacePreferencesDto>(access.Error);

        var now = timeProvider.GetUtcNow();
        var changed = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO messaging.space_user_states
                (space_id, user_id, notification_level, muted_until, is_hidden, is_pinned, updated_at)
            VALUES ({spaceId}, {actorUserId}, {preferences.NotificationLevel}, {preferences.MutedUntil},
                    {preferences.IsHidden}, {preferences.IsPinned}, {now})
            ON CONFLICT (space_id, user_id) DO UPDATE SET
                notification_level = EXCLUDED.notification_level,
                muted_until = EXCLUDED.muted_until,
                is_hidden = EXCLUDED.is_hidden,
                is_pinned = EXCLUDED.is_pinned
            WHERE messaging.space_user_states.notification_level IS DISTINCT FROM EXCLUDED.notification_level
               OR messaging.space_user_states.muted_until IS DISTINCT FROM EXCLUDED.muted_until
               OR messaging.space_user_states.is_hidden IS DISTINCT FROM EXCLUDED.is_hidden
               OR messaging.space_user_states.is_pinned IS DISTINCT FROM EXCLUDED.is_pinned
            """, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        if (changed > 0)
        {
            try
            {
                await realtimeEvents.NotifyPreferencesUpdatedAsync(actorUserId, spaceId, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Could not publish preferences for space {SpaceId}", spaceId);
            }
        }

        return Result.Success(preferences);
    }

    private async Task<Result> CheckAccessAsync(Guid actorUserId, Guid spaceId, CancellationToken cancellationToken)
    {
        if (actorUserId == Guid.Empty || await userDirectory.FindByIdAsync(actorUserId, cancellationToken) is null)
            return Result.Failure(MessagingErrors.AccountUnavailable);
        if (spaceId == Guid.Empty) return Result.Failure(MessagingErrors.ResourceNotFound);
        var space = await dbContext.Spaces.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == spaceId && item.Status != SpaceStatus.Deleted, cancellationToken);
        return space is not null && (await spaceAccess.CheckAsync(actorUserId, space, cancellationToken)).CanRead
            ? Result.Success()
            : Result.Failure(MessagingErrors.ResourceNotFound);
    }

    private static UserSpacePreferencesDto ToDto(SpaceUserState? state) => new(
        (short)(state?.NotificationLevel ?? NotificationLevel.AllMessages),
        state?.MutedUntil,
        state?.IsHidden ?? false,
        state?.IsPinned ?? false);
}
