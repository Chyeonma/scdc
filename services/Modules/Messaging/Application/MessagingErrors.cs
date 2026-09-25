using SCDC.BuildingBlocks.Application.Results;

namespace SCDC.Modules.Messaging.Application;

public static class MessagingErrors
{
    public static readonly Error InvalidRecipient = Error.Validation(
        "Messaging.ValidationFailed",
        "A recipient user ID is required.");

    public static readonly Error SelfConversationNotAllowed = Error.Validation(
        "Messaging.SelfConversationNotAllowed",
        "You cannot start a direct conversation with yourself.");

    public static readonly Error AccountUnavailable = Error.Forbidden(
        "Messaging.AccountUnavailable",
        "Messaging is not available for this account.");

    public static readonly Error DirectConversationUnavailable = Error.Forbidden(
        "Messaging.DirectConversationUnavailable",
        "The direct conversation is unavailable.");

    public static readonly Error ResourceNotFound = Error.NotFound(
        "Messaging.ResourceNotFound",
        "The requested resource was not found.");

    public static readonly Error DirectConversationClosed = Error.Conflict(
        "Messaging.DirectConversationClosed",
        "The direct conversation is no longer available.");

    public static readonly Error DirectConversationConflict = Error.Conflict(
        "Messaging.DirectConversationConflict",
        "The direct conversation could not be created.");

    public static readonly Error InvalidCursor = Error.Validation(
        "Messaging.InvalidCursor",
        "The conversation cursor is invalid.");

    public static readonly Error InvalidPageSize = Error.Validation(
        "Messaging.InvalidPageSize",
        "The conversation page size must be between 1 and 100.");

    public static readonly Error InvalidMessage = Error.Validation(
        "Messaging.ValidationFailed",
        "The message content or type is invalid.");

    public static readonly Error InvalidAttachment = Error.Validation(
        "Messaging.InvalidAttachment", "The attachment is invalid, too large, or has an unsupported format.");

    public static readonly Error AttachmentUnavailable = Error.Conflict(
        "Messaging.AttachmentUnavailable", "The attachment upload is unavailable for this message.");

    public static readonly Error AttachmentRejected = Error.Validation(
        "Messaging.AttachmentRejected", "The attachment did not pass the security scan.");

    public static readonly Error AttachmentScanUnavailable = Error.ServiceUnavailable(
        "Messaging.AttachmentScanUnavailable", "Attachment scanning is temporarily unavailable.");

    public static readonly Error AttachmentStorageUnavailable = Error.ServiceUnavailable(
        "Messaging.AttachmentStorageUnavailable", "Attachment storage is temporarily unavailable.");

    public static readonly Error AttachmentUploadLimitReached = Error.TooManyRequests(
        "Messaging.AttachmentUploadLimitReached", "Too many unattached uploads are pending for this account.");

    public static readonly Error ActionNotAllowed = Error.Forbidden(
        "Messaging.ActionNotAllowed",
        "You cannot perform this action in the space.");

    public static readonly Error SpaceNotWritable = Error.Conflict(
        "Messaging.SpaceNotWritable",
        "The space is not writable.");

    public static readonly Error VersionConflict = Error.Conflict(
        "Messaging.VersionConflict", "The message changed. Refresh it and try again.");

    public static readonly Error MessageDeleted = Error.Conflict(
        "Messaging.MessageDeleted", "The message was deleted.");

    public static readonly Error IdempotencyConflict = Error.Conflict(
        "Messaging.IdempotencyConflict",
        "The client message ID was already used with a different payload.");

    public static readonly Error RateLimited = Error.TooManyRequests(
        "Messaging.RateLimited",
        "Too many messages were sent. Try again shortly.");

    public static readonly Error InvalidMessageCursor = Error.Validation(
        "Messaging.InvalidCursor",
        "The message history cursor is invalid.");

    public static readonly Error InvalidReadState = Error.Validation(
        "Messaging.InvalidReadState",
        "The read position must identify a message in this space.");

    public static readonly Error InvalidPreferences = Error.Validation(
        "Messaging.InvalidPreferences",
        "The space notification preferences are invalid.");

    public static readonly Error InvalidGroup = Error.Validation(
        "Messaging.InvalidGroup",
        "The group name, members, or member limit is invalid.");

    public static readonly Error GroupMemberUnavailable = Error.Validation(
        "Messaging.GroupMemberUnavailable",
        "One or more group members are unavailable.");

    public static readonly Error GroupMemberLimitReached = Error.Conflict(
        "Messaging.GroupMemberLimitReached",
        "The group has reached its member limit.");

    public static readonly Error GroupOwnerTransferRequired = Error.Conflict(
        "Messaging.GroupOwnerTransferRequired",
        "Transfer group ownership before leaving the group.");
}
