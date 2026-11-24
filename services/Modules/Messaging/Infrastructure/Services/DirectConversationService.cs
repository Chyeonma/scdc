using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.BuildingBlocks.Infrastructure;
using SCDC.Contracts.Identity;
using SCDC.Contracts.Persistence;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Domain;
using SCDC.Modules.Messaging.Infrastructure.Access;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class DirectConversationService(MessagingDbContext db, MessagingSession session,
    MessagingIdentityAccess access, MessagingProfiles profiles, TimeProvider clock) : IDirectConversationService
{
    public async Task<Result<DirectConversationResponse>> OpenAsync(OpenDirectConversation command, CancellationToken cancellationToken)
    {
        if (command.PeerUserId == Guid.Empty) return Result.Failure<DirectConversationResponse>(Error.Validation("Common.ValidationFailed", "peerUserId must be a non-empty UUID."));
        var pair = UuidNetworkOrder.Pair(command.ActorId, command.PeerUserId);
        var actor = new AuthenticatedSession(command.ActorId, command.SessionId, command.SecurityStamp, command.AccessExpiresAt);
        // Discovery is a hint, never authorization. Classify again after a unique loser or changed hint.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                var hint = await db.DirectConversations.AsNoTracking().SingleOrDefaultAsync(d => d.UserLowId == pair.Low && d.UserHighId == pair.High, cancellationToken);
                var creating = hint is null && command.ActorId != command.PeerUserId;
                var resource = hint?.SpaceId ?? Guid.CreateVersion7();
                IdentityPermit permit;
                try { permit = await access.AcquireAsync(actor, creating ? IdentityPurpose.DmOpenNew : IdentityPurpose.DmRead, resource, cancellationToken, creating ? command.PeerUserId : null); }
                catch (MessagingAccessFailure e) when (creating && e.Error.Code == "RESOURCE_NOT_FOUND")
                {
                    if (await db.DirectConversations.AsNoTracking().AnyAsync(d => d.UserLowId == pair.Low && d.UserHighId == pair.High, cancellationToken)) continue;
                    throw;
                }
                if (command.ActorId == command.PeerUserId) return Result.Failure<DirectConversationResponse>(Error.Validation("INVALID_PEER", "Choose another person."));
                var people = await profiles.ReadAsync(new[] { command.ActorId, command.PeerUserId }, cancellationToken);
                await using var owner = await session.BeginAsync(cancellationToken);
                try
                {
                    await db.Database.UseTransactionAsync(session.Transaction, cancellationToken);
                    await access.AdmitAsync(db, permit, cancellationToken);
                    var existing = await db.DirectConversations.AsNoTracking().SingleOrDefaultAsync(d => d.UserLowId == pair.Low && d.UserHighId == pair.High, cancellationToken);
                    ChatSpace space;
                    if (existing is not null)
                    {
                        if (creating || existing.SpaceId != resource) continue;
                        await db.Database.SqlQuery<Guid>($"SELECT id AS \"Value\" FROM messaging.spaces WHERE id={existing.SpaceId} FOR SHARE").ToListAsync(cancellationToken);
                        var current = await db.Spaces.AsNoTracking().SingleOrDefaultAsync(s => s.Id == existing.SpaceId && s.SpaceType == 1 && s.Status != 3 && s.DeletedAt == null, cancellationToken);
                        var members = await db.SpaceMembers.AsNoTracking().Where(m => m.SpaceId == existing.SpaceId && m.MembershipStatus == 1 && m.LeftAt == null).Select(m => m.UserId).ToListAsync(cancellationToken);
                        if (current is null || members.Count != 2 || !members.Contains(command.ActorId) || !members.Contains(command.PeerUserId)) return NotFound();
                        space = current;
                    }
                    else
                    {
                        if (!creating) continue; // A read permit never creates a conversation.
                        var now = clock.GetUtcNow();
                        space = new ChatSpace { Id = resource, CreatedByUserId = command.ActorId, CreatedAt = now };
                        db.Spaces.Add(space); await db.SaveChangesAsync(cancellationToken);
                        db.DirectConversations.Add(new DirectConversation { SpaceId = space.Id, UserLowId = pair.Low, UserHighId = pair.High, CreatedAt = now });
                        db.SpaceMembers.AddRange(new SpaceMember { SpaceId = space.Id, UserId = pair.Low, JoinedAt = now }, new SpaceMember { SpaceId = space.Id, UserId = pair.High, JoinedAt = now });
                        await db.SaveChangesAsync(cancellationToken);
                    }
                    var participants = new[] { people[pair.Low], people[pair.High] };
                    var response = new DirectConversationResponse(space.Id, participants, space.CreatedAt, (space.LastMessageSequence ?? 0).ToString(CultureInfo.InvariantCulture), space.LastActivityAt);
                    await access.EnsureAsync(db, permit, cancellationToken); await owner.CommitAsync(cancellationToken);
                    return Result.Success(response);
                }
                finally { await db.Database.UseTransactionAsync(null, CancellationToken.None); }
            }
            catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ux_direct_conversations_pair" }) { db.ChangeTracker.Clear(); }
            catch (MessagingAccessFailure e) { return Result.Failure<DirectConversationResponse>(e.Error); }
            catch (Exception e) when (MessagingFailures.Temporary(e)) { return Unavailable(); }
        }
        return Unavailable();
    }
    private static Result<DirectConversationResponse> NotFound() => Result.Failure<DirectConversationResponse>(Error.NotFound("RESOURCE_NOT_FOUND", "The requested conversation is not available."));
    private static Result<DirectConversationResponse> Unavailable() => Result.Failure<DirectConversationResponse>(Error.ServiceUnavailable("AUTHORITY_UNAVAILABLE", "The service cannot confirm this request. Please try again."));
}
