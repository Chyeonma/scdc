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
}
