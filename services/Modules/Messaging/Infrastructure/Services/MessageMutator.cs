using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.BuildingBlocks.Infrastructure;
using SCDC.Contracts.Identity;
using SCDC.Contracts.Persistence;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Domain;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class MessageMutator(MessagingDbContext db, ISharedDatabaseSession session,
    IAccountPairAccessGuard guard, TimeProvider clock) : IMessageMutator
{
    public async Task<Result<MessageResponse>> MutateAsync(MutateMessage command, CancellationToken token)
    {
        try
        {
            await using var owner = await session.BeginAsync(token);
            try
            {
                await db.Database.UseTransactionAsync(session.Transaction, token);
                // Author mutations retain access when the peer is disabled. Lock only the actor,
                // in the same Identity -> session -> space order used by readers/writers.
                var access = await guard.LockPairAsync(new(command.ActorId, command.SessionId, command.SecurityStamp, command.ActorId), token);
                if (access.Failure != null) return Fail(Error.Unauthorized("Common.Unauthorized", "The current session is not available."));
                var pair = await db.DirectConversations.AsNoTracking().SingleOrDefaultAsync(p => p.SpaceId == command.ConversationId, token);
                if (pair == null || pair.UserLowId != command.ActorId && pair.UserHighId != command.ActorId) return Missing();
                await db.Database.SqlQuery<Guid>($"SELECT id AS \"Value\" FROM messaging.spaces WHERE id={command.ConversationId} FOR UPDATE").ToListAsync(token);
                var space = await db.Spaces.AsNoTracking().AnyAsync(s => s.Id == command.ConversationId && s.SpaceType == 1 && s.Status != 3 && s.DeletedAt == null, token);
                var members = await db.SpaceMembers.AsNoTracking().Where(m => m.SpaceId == command.ConversationId && m.MembershipStatus == 1 && m.LeftAt == null).Select(m => m.UserId).ToListAsync(token);
                if (!space || members.Count != 2 || !members.Contains(pair.UserLowId) || !members.Contains(pair.UserHighId)) return Missing();
                var message = await db.Messages.SingleOrDefaultAsync(m => m.Id == command.MessageId && m.SpaceId == command.ConversationId, token);
                if (message == null) return Missing();
                if (message.AuthorUserId != command.ActorId) return Fail(Error.Forbidden("AUTHOR_REQUIRED", "Only the author can change this message."));
                if (string.IsNullOrEmpty(command.ExpectedVersion) || command.ExpectedVersion[0] == '0'
                    || !command.ExpectedVersion.All(c => c is >= '0' and <= '9')
                    || !BigInteger.TryParse(command.ExpectedVersion, NumberStyles.None, CultureInfo.InvariantCulture, out var expected))
                    return Fail(Error.Validation("Common.ValidationFailed", "expectedVersion must be a positive decimal string."));
                if (!command.Delete && message.DeletedAt != null) return Fail(Error.Conflict("MESSAGE_DELETED", "This message was deleted."));
                if (message.DeletedAt == null)
                {
                    if (expected != message.Version) return Fail(Error.Conflict("VERSION_CONFLICT", "Reload the current message before changing it."));
                    string? content = null;
                    if (!command.Delete)
                    {
                        var validated = TextContent.Validate(command.Content);
                        if (validated.Error != null) return Fail(validated.Error);
                        content = validated.Content;
                    }
                    if (command.Delete || message.Content != content)
                    {
                        if (message.Version == int.MaxValue) return Unavailable();
                        var time = clock.GetUtcNow();
                        // PostgreSQL stores microseconds; return the same timestamp on a later no-op/replay.
                        var now = new DateTimeOffset(time.Ticks - time.Ticks % 10, time.Offset); message.Version++;
                        message.Content = content;
                        if (command.Delete) message.DeletedAt = now;
                        else message.EditedAt = now;
                        db.MessageOutbox.Add(new MessageOutbox { Id = Guid.CreateVersion7(), AggregateId = message.Id,
                            AggregateVersion = message.Version, SpaceId = message.SpaceId, OccurredAt = now, AvailableAt = now,
                            Payload = JsonSerializer.Serialize(new { messageId = message.Id, conversationId = message.SpaceId,
                                version = message.Version.ToString(CultureInfo.InvariantCulture) }) });
                        await db.SaveChangesAsync(token);
                    }
                }
                var response = new MessageResponse(message.Id, message.SpaceId, access.Actor!, message.ClientMessageId,
                    message.ConversationSequence.ToString(CultureInfo.InvariantCulture), message.Version.ToString(CultureInfo.InvariantCulture),
                    message.DeletedAt == null ? message.Content : null, message.CreatedAt, message.EditedAt, message.DeletedAt);
                await owner.CommitAsync(token); return Result.Success(response);
            }
            finally { await db.Database.UseTransactionAsync(null, CancellationToken.None); await guard.DetachAsync(CancellationToken.None); }
        }
        catch (Exception e) when (DatabaseAvailability.IsUnavailable(e)) { return Unavailable(); }
    }
    private static Result<MessageResponse> Fail(Error e) => Result.Failure<MessageResponse>(e);
    private static Result<MessageResponse> Missing() => Fail(Error.NotFound("RESOURCE_NOT_FOUND", "The requested message is not available."));
    private static Result<MessageResponse> Unavailable() => Fail(Error.ServiceUnavailable("AUTHORITY_UNAVAILABLE", "The service cannot confirm this request."));
}
