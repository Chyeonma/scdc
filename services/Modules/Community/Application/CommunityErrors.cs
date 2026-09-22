using SCDC.BuildingBlocks.Application.Results;

namespace SCDC.Modules.Community.Application;

internal static class CommunityErrors
{
    public static readonly Error Invalid = Error.Validation("Community.ValidationFailed", "The community request is invalid.");
    public static readonly Error NotFound = Error.NotFound("Community.ResourceNotFound", "The requested community resource was not found.");
    public static readonly Error Forbidden = Error.Forbidden("Community.ActionNotAllowed", "You cannot perform this action in the server.");
    public static readonly Error Conflict = Error.Conflict("Community.Conflict", "The community resource conflicts with an existing resource.");
    public static readonly Error ProvisioningFailed = Error.ServiceUnavailable("Community.ChannelProvisioningFailed", "The chat space could not be provisioned; no channel was created.");
}
