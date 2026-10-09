using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.BuildingBlocks.Infrastructure;
using SCDC.Contracts.Identity;
using SCDC.Contracts.Persistence;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Domain;
using SCDC.Modules.Messaging.Infrastructure.Persistence;
using SCDC.Modules.Messaging.Infrastructure.Security;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class TextMessageSender(MessagingDbContext db, ISharedDatabaseSession session,
    IAccountAccessGuard guard, MessageFingerprint fingerprints, TimeProvider clock) : ITextMessageSender
{
    public async Task<Result<MessageResponse>> SendAsync(SendTextMessage command, CancellationToken cancellationToken)
    {
        try
        {
            await using var owner = await session.BeginAsync(cancellationToken);
            try
            {
                await db.Database.UseTransactionAsync(session.Transaction, cancellationToken);
                // Discover the pair without a space lock: ordered Identity locks must precede the space lock.
                var pair = await db.DirectConversations.AsNoTracking().SingleOrDefaultAsync(
                    p => p.SpaceId == command.ConversationId, cancellationToken);
                var belongs = pair is not null && (pair.UserLowId == command.ActorId || pair.UserHighId == command.ActorId);
                var peer = belongs ? (pair!.UserLowId == command.ActorId ? pair.UserHighId : pair.UserLowId) : command.ActorId;
                var access = await guard.LockPairAsync(new(command.ActorId, command.SessionId, command.SecurityStamp, peer), cancellationToken);
                if (access.Failure is not null) return Fail(Error.Unauthorized("Common.Unauthorized", "The current session is not available."));
                if (!belongs) return NotFound();
                await db.Database.SqlQuery<Guid>($"""
                    SELECT id AS "Value" FROM messaging.spaces WHERE id = {command.ConversationId} FOR UPDATE
                    """).ToListAsync(cancellationToken);
                var space = await db.Spaces.SingleOrDefaultAsync(s => s.Id == command.ConversationId
                    && s.SpaceType == 1 && s.Status != 3 && s.DeletedAt == null, cancellationToken);
                var members = await db.SpaceMembers.AsNoTracking().Where(m => m.SpaceId == command.ConversationId
                    && m.MembershipStatus == 1 && m.LeftAt == null).Select(m => m.UserId).ToListAsync(cancellationToken);
                if (space is null || members.Count != 2 || !members.Contains(command.ActorId) || !members.Contains(peer)) return NotFound();
                if (command.ClientMessageId == Guid.Empty || command.ClientMessageId.Version != 4)
                    return Fail(Error.Validation("Common.ValidationFailed", "clientMessageId must be a UUID v4."));
                var validated = TextContent.Validate(command.Content);
                if (validated.Error is not null) return Fail(validated.Error);
                var content = validated.Content!;
                var operation = await db.SendOperations.AsNoTracking().SingleOrDefaultAsync(o => o.SpaceId == command.ConversationId
                    && o.AuthorUserId == command.ActorId && o.ClientMessageId == command.ClientMessageId, cancellationToken);
                TextMessage message;
                if (operation is not null)
                {
                    if (operation.FingerprintVersion != 1 || operation.KeyId is null || operation.Fingerprint is null)
                        return Fail(Error.Conflict("OPERATION_UNVERIFIABLE", "This operation has no verifiable original fingerprint."));
                    var hash = fingerprints.Compute(operation.KeyId, command.ConversationId, command.ActorId, command.ClientMessageId, content);
                    if (hash is null) return KeyUnavailable();
                    if (!CryptographicOperations.FixedTimeEquals(hash, operation.Fingerprint))
                        return Fail(Error.Conflict("OPERATION_CONFLICT", "This clientMessageId was used for different content."));
                    message = await db.Messages.AsNoTracking().SingleAsync(m => m.Id == operation.MessageId && m.SpaceId == command.ConversationId, cancellationToken);
                }
                else
                {
                    if (!access.PeerEligible) return NotFound();
                    var keyId = fingerprints.ActiveKeyId;
                    var hash = fingerprints.Compute(keyId, command.ConversationId, command.ActorId, command.ClientMessageId, content);
                    if (hash is null) return KeyUnavailable();
                    if (space.LastMessageSequence == long.MaxValue) return Unavailable();
                    var now = clock.GetUtcNow();
                    message = new TextMessage { Id = Guid.CreateVersion7(), SpaceId = space.Id, AuthorUserId = command.ActorId,
                        ClientMessageId = command.ClientMessageId, Content = content, CreatedAt = now,
                        ConversationSequence = (space.LastMessageSequence ?? 0) + 1 };
                    db.Messages.Add(message);
                    await db.SaveChangesAsync(cancellationToken);
                    db.SendOperations.Add(new SendOperation { SpaceId = space.Id, AuthorUserId = command.ActorId,
                        ClientMessageId = command.ClientMessageId, MessageId = message.Id, FingerprintVersion = 1,
                        KeyId = keyId, Fingerprint = hash, CreatedAt = now });
                    db.MessageOutbox.Add(new MessageOutbox { Id = Guid.CreateVersion7(), AggregateId = message.Id,
                        SpaceId = space.Id, OccurredAt = now, AvailableAt = now,
                        Payload = JsonSerializer.Serialize(new { messageId = message.Id, conversationId = space.Id, version = "1" }) });
                    space.LastMessageSequence = message.ConversationSequence;
                    space.LastMessageId = message.Id;
                    space.LastActivityAt = now;
                    await db.SaveChangesAsync(cancellationToken);
                }
                var response = new MessageResponse(message.Id, message.SpaceId, access.Actor!, message.ClientMessageId,
                    message.ConversationSequence.ToString(CultureInfo.InvariantCulture), message.Version.ToString(CultureInfo.InvariantCulture),
                    message.Content, message.CreatedAt, message.EditedAt, message.DeletedAt);
                await owner.CommitAsync(cancellationToken);
                return Result.Success(response);
            }
            finally
            {
                await db.Database.UseTransactionAsync(null, CancellationToken.None);
                await guard.DetachAsync(CancellationToken.None);
            }
        }
        catch (Exception exception) when (DatabaseAvailability.IsUnavailable(exception)) { return Unavailable(); }
    }
    private static Result<MessageResponse> Fail(Error error) => Result.Failure<MessageResponse>(error);
    private static Result<MessageResponse> NotFound() => Fail(Error.NotFound("RESOURCE_NOT_FOUND", "The requested conversation is not available."));
    private static Result<MessageResponse> Unavailable() => Fail(Error.ServiceUnavailable("AUTHORITY_UNAVAILABLE", "The service cannot confirm this request. Please try again."));
    private static Result<MessageResponse> KeyUnavailable() => Fail(Error.ServiceUnavailable("FINGERPRINT_KEY_UNAVAILABLE", "The operation fingerprint key is not available."));
}
