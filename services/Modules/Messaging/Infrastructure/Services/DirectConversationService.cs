using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.BuildingBlocks.Infrastructure;
using SCDC.Contracts.Identity;
using SCDC.Contracts.Persistence;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Domain;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class DirectConversationService(MessagingDbContext db, ISharedDatabaseSession session,
    IAccountAccessGuard guard, TimeProvider clock) : IDirectConversationService
{
    public async Task<Result<DirectConversationResponse>> OpenAsync(OpenDirectConversation command, CancellationToken cancellationToken)
    {
        if (command.PeerUserId == Guid.Empty)
            return Result.Failure<DirectConversationResponse>(Error.Validation("Common.ValidationFailed", "peerUserId must be a non-empty UUID."));
        var pair = UuidNetworkOrder.Pair(command.ActorId, command.PeerUserId);
        // A losing unique insert rolls back its entire space/member transaction, then
        // reacquires authorization and reads the committed winning pair in a new transaction.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                await using var owner = await session.BeginAsync(cancellationToken);
                try
                {
                    await db.Database.UseTransactionAsync(session.Transaction, cancellationToken);
                    var access = await guard.LockPairAsync(new(command.ActorId, command.SessionId,
                        command.SecurityStamp, command.PeerUserId), cancellationToken);
                    if (access.Failure is not null)
                        return Result.Failure<DirectConversationResponse>(Error.Unauthorized("Common.Unauthorized", "The current session is not available."));
                    if (command.ActorId == command.PeerUserId)
                        return Result.Failure<DirectConversationResponse>(Error.Validation("INVALID_PEER", "Choose another person."));

                    var existing = await db.DirectConversations.AsNoTracking().SingleOrDefaultAsync(d =>
                        d.UserLowId == pair.Low && d.UserHighId == pair.High, cancellationToken);
                    ChatSpace? space;
                    if (existing is not null)
                    {
                        await db.Database.SqlQuery<Guid>($"""
                            SELECT id AS "Value" FROM messaging.spaces WHERE id = {existing.SpaceId} FOR SHARE
                            """).ToListAsync(cancellationToken);
                        space = await db.Spaces.AsNoTracking().SingleOrDefaultAsync(s =>
                            s.Id == existing.SpaceId && s.SpaceType == 1 && s.Status != 3 && s.DeletedAt == null, cancellationToken);
                        var members = await db.SpaceMembers.AsNoTracking().Where(m => m.SpaceId == existing.SpaceId
                            && m.MembershipStatus == 1 && m.LeftAt == null).Select(m => m.UserId).ToListAsync(cancellationToken);
                        if (space is null || members.Count != 2 || !members.Contains(command.ActorId)
                            || !members.Contains(command.PeerUserId) || access.Peer is null) return NotFound();
                    }
                    else
                    {
                        if (!access.PeerEligible || access.Peer is null) return NotFound();
                        var now = clock.GetUtcNow();
                        space = new ChatSpace { Id = Guid.CreateVersion7(), CreatedByUserId = command.ActorId, CreatedAt = now };
                        db.Spaces.Add(space);
                        await db.SaveChangesAsync(cancellationToken);
                        db.DirectConversations.Add(new DirectConversation { SpaceId = space.Id,
                            UserLowId = pair.Low, UserHighId = pair.High, CreatedAt = now });
                        db.SpaceMembers.AddRange(
                            new SpaceMember { SpaceId = space.Id, UserId = pair.Low, JoinedAt = now },
                            new SpaceMember { SpaceId = space.Id, UserId = pair.High, JoinedAt = now });
                        await db.SaveChangesAsync(cancellationToken);
                    }
                    var participants = UuidNetworkOrder.Compare(access.Actor!.Id, access.Peer!.Id) < 0
                        ? new[] { access.Actor!, access.Peer! } : new[] { access.Peer!, access.Actor! };
                    var response = new DirectConversationResponse(space.Id, participants, space.CreatedAt,
                        (space.LastMessageSequence ?? 0).ToString(CultureInfo.InvariantCulture), space.LastActivityAt);
                    await owner.CommitAsync(cancellationToken);
                    return Result.Success(response);
                }
                finally
                {
                    await db.Database.UseTransactionAsync(null, CancellationToken.None);
                    await guard.DetachAsync(CancellationToken.None);
                }
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException
                { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ux_direct_conversations_pair" })
            {
                db.ChangeTracker.Clear();
            }
            catch (Exception exception) when (DatabaseAvailability.IsUnavailable(exception))
            {
                return Unavailable();
            }
        }
        return Unavailable();
    }

    private static Result<DirectConversationResponse> NotFound() => Result.Failure<DirectConversationResponse>(
        Error.NotFound("RESOURCE_NOT_FOUND", "The requested conversation is not available."));
    private static Result<DirectConversationResponse> Unavailable() => Result.Failure<DirectConversationResponse>(
        Error.ServiceUnavailable("AUTHORITY_UNAVAILABLE", "The service cannot confirm this request. Please try again."));
}
