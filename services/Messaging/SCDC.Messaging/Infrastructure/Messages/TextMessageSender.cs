using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.BuildingBlocks.Infrastructure;
using SCDC.Contracts.Identity;
using SCDC.Modules.Messaging.Infrastructure.Access;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Domain;
using SCDC.Modules.Messaging.Infrastructure.Persistence;
using SCDC.Modules.Messaging.Infrastructure.Security;

namespace SCDC.Modules.Messaging.Infrastructure.Messages;

internal sealed class TextMessageSender(MessagingDbContext db, MessagingSession session,
    MessagingIdentityAccess access, MessagingProfiles profiles, MessageFingerprint fingerprints,
    PrivateConversationMembership membership, TimeProvider clock) : ITextMessageSender
{
    public async Task<Result<MessageResponse>> SendAsync(SendTextMessage command, CancellationToken cancellationToken)
    {
        try
        {
            var actor = new AuthenticatedSession(command.ActorId, command.SessionId, command.SecurityStamp, command.AccessExpiresAt);
            var group = command.Kind == ConversationKind.Group;
            var hint = group ? null : await db.DirectConversations.AsNoTracking().SingleOrDefaultAsync(p => p.SpaceId == command.ConversationId, cancellationToken);
            var belongsHint = hint is not null && (hint.UserLowId == command.ActorId || hint.UserHighId == command.ActorId);
            var peerHint = belongsHint ? (hint!.UserLowId == command.ActorId ? hint.UserHighId : hint.UserLowId) : (Guid?)null;
            var replayHint = !group && await db.SendOperations.AsNoTracking().AnyAsync(o => o.SpaceId == command.ConversationId && o.AuthorUserId == command.ActorId && o.ClientMessageId == command.ClientMessageId, cancellationToken);
            var purpose = group
                ? IdentityPurpose.DmRead
                : !belongsHint ? IdentityPurpose.DmRead : replayHint ? IdentityPurpose.DmReplay : IdentityPurpose.DmSendNew;
            IdentityPermit permit;
            try
            {
                permit = await access.AcquireAsync(actor, purpose, command.ConversationId, cancellationToken, purpose == IdentityPurpose.DmSendNew ? peerHint : null);
            }
            catch (MessagingAccessFailure e) when (purpose == IdentityPurpose.DmSendNew && e.Error.Code == "RESOURCE_NOT_FOUND")
            {
                // A concurrent request may have committed this operation before the peer became unavailable.
                // Reclassify only an existing operation; the transaction still forbids creating with a replay permit.
                if (!await db.SendOperations.AsNoTracking().AnyAsync(o => o.SpaceId == command.ConversationId && o.AuthorUserId == command.ActorId && o.ClientMessageId == command.ClientMessageId, cancellationToken)) throw;
                permit = await access.AcquireAsync(actor, IdentityPurpose.DmReplay, command.ConversationId, cancellationToken);
            }
            var people = await profiles.ReadAsync(new[] { command.ActorId }, cancellationToken);
            await using var owner = await session.BeginAsync(cancellationToken);
            try
            {
                await db.Database.UseTransactionAsync(session.Transaction, cancellationToken);
                await access.AdmitAsync(db, permit, cancellationToken);
                var current = await membership.LockAsync(command.ActorId, command.ConversationId, command.Kind, write: true, cancellationToken);
                if (current is null) return NotFound();
                if (peerHint != current.PeerId) return Fail(Error.Conflict("ACCESS_CHANGED", "The conversation changed. Reload before sending."));
                var space = current.Space;
                if (command.ClientMessageId == Guid.Empty || command.ClientMessageId.Version != 4)
                    return Fail(Error.Validation("Common.ValidationFailed", "clientMessageId must be a UUID v4."));
                var validated = TextContent.Validate(command.Content);
                if (validated.Error is not null) return Fail(validated.Error);
                var content = validated.Content!;
                var operation = await db.SendOperations.AsNoTracking().SingleOrDefaultAsync(o => o.SpaceId == command.ConversationId
                    && o.AuthorUserId == command.ActorId && o.ClientMessageId == command.ClientMessageId, cancellationToken);
                TextMessage message;
                MessageReply? reply;
                if (operation is not null)
                {
                    if (operation.FingerprintVersion != 1 || operation.KeyId is null || operation.Fingerprint is null)
                        return Fail(Error.Conflict("OPERATION_UNVERIFIABLE", "This operation has no verifiable original fingerprint."));
                    var hash = fingerprints.Compute(operation.KeyId, command.ConversationId, command.ActorId, command.ClientMessageId, content);
                    if (hash is null) return KeyUnavailable();
                    if (!CryptographicOperations.FixedTimeEquals(hash, operation.Fingerprint))
                        return Fail(Error.Conflict("OPERATION_CONFLICT", "This clientMessageId was used for different content."));
                    message = await db.Messages.AsNoTracking().SingleAsync(m => m.Id == operation.MessageId && m.SpaceId == command.ConversationId, cancellationToken);
                    // The reply target is immutable and committed together with the operation fingerprint.
                    if (message.ReplyToMessageId != command.ReplyToMessageId)
                        return Fail(Error.Conflict("OPERATION_CONFLICT", "This clientMessageId was used for a different reply target."));
                    reply = await ReplyPreviews.ReadAsync(db, space.Id, message.ReplyToMessageId, cancellationToken);
                }
                else
                {
                    // A group write is authorized by the caller's permit and the local member lock.
                    var sendPurpose = group ? IdentityPurpose.DmRead : IdentityPurpose.DmSendNew;
                    if (permit.Request.Purpose != sendPurpose) return Fail(Error.Conflict("ACCESS_CHANGED", "The original operation changed. Reconcile before sending."));
                    var keyId = fingerprints.ActiveKeyId;
                    var hash = fingerprints.Compute(keyId, command.ConversationId, command.ActorId, command.ClientMessageId, content);
                    if (hash is null) return KeyUnavailable();
                    reply = await ReplyPreviews.ReadAsync(db, space.Id, command.ReplyToMessageId, cancellationToken);
                    if (command.ReplyToMessageId.HasValue && (reply is null || reply.IsDeleted))
                        return Fail(Error.NotFound("REPLY_TARGET_NOT_FOUND", "The message being replied to is not available."));
                    if (space.LastMessageSequence == long.MaxValue) return Unavailable();
                    var now = clock.GetUtcNow();
                    message = new TextMessage
                    {
                        Id = Guid.CreateVersion7(),
                        SpaceId = space.Id,
                        AuthorUserId = command.ActorId,
                        ClientMessageId = command.ClientMessageId,
                        Content = content,
                        ReplyToMessageId = command.ReplyToMessageId,
                        CreatedAt = now,
                        ConversationSequence = (space.LastMessageSequence ?? 0) + 1
                    };
                    db.Messages.Add(message);
                    await db.SaveChangesAsync(cancellationToken);
                    db.SendOperations.Add(new SendOperation
                    {
                        SpaceId = space.Id,
                        AuthorUserId = command.ActorId,
                        ClientMessageId = command.ClientMessageId,
                        MessageId = message.Id,
                        FingerprintVersion = 1,
                        KeyId = keyId,
                        Fingerprint = hash,
                        CreatedAt = now
                    });
                    db.MessageOutbox.Add(new MessageOutbox
                    {
                        Id = Guid.CreateVersion7(),
                        AggregateId = message.Id,
                        SpaceId = space.Id,
                        OccurredAt = now,
                        AvailableAt = now,
                        Payload = JsonSerializer.Serialize(new { messageId = message.Id, conversationId = space.Id, version = "1" })
                    });
                    space.LastMessageSequence = message.ConversationSequence;
                    space.LastMessageId = message.Id;
                    space.LastActivityAt = now;
                    await db.SaveChangesAsync(cancellationToken);
                }
                var response = new MessageResponse(message.Id, message.SpaceId, people[command.ActorId], message.ClientMessageId,
                    message.ConversationSequence.ToString(CultureInfo.InvariantCulture), message.Version.ToString(CultureInfo.InvariantCulture),
                    message.Content, message.CreatedAt, message.EditedAt, message.DeletedAt,
                    Reply: message.DeletedAt is null ? reply : null);
                await access.EnsureAsync(db, permit, cancellationToken);
                await owner.CommitAsync(cancellationToken);
                return Result.Success(response);
            }
            finally
            {
                await db.Database.UseTransactionAsync(null, CancellationToken.None);

            }
        }
        catch (MessagingAccessFailure e) { return Fail(e.Error); }
        catch (Exception exception) when (MessagingFailures.Temporary(exception)) { return Unavailable(); }
    }
    private static Result<MessageResponse> Fail(Error error) => Result.Failure<MessageResponse>(error);
    private static Result<MessageResponse> NotFound() => Fail(Error.NotFound("RESOURCE_NOT_FOUND", "The requested conversation is not available."));
    private static Result<MessageResponse> Unavailable() => Fail(Error.ServiceUnavailable("AUTHORITY_UNAVAILABLE", "The service cannot confirm this request. Please try again."));
    private static Result<MessageResponse> KeyUnavailable() => Fail(Error.ServiceUnavailable("FINGERPRINT_KEY_UNAVAILABLE", "The operation fingerprint key is not available."));
}
