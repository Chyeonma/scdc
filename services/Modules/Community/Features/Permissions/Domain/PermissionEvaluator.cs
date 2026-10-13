namespace SCDC.Modules.Community.Features.Permissions.Domain;

internal static class ManagementPermissions
{
    // Order is the published fingerprint bit order, not a hierarchy.
    public static IReadOnlyList<string> Codes { get; } = Array.AsReadOnly<string>(
        ["manage_channels", "manage_invites", "review_join_requests", "manage_join_mode", "manage_channel_access"]);
    public static bool Contains(string value) => Codes.Contains(value, StringComparer.Ordinal);
    public static byte Mask(IEnumerable<string> permissions)
    {
        byte mask = 0;
        foreach (var permission in permissions)
        {
            var index = 0;
            while (index < Codes.Count && Codes[index] != permission) index++;
            if (index == Codes.Count) throw new ArgumentException("Unknown management permission.");
            mask |= (byte)(1 << index);
        }
        return mask;
    }
}
internal enum ViewEffect { Inherit, Allow, Deny }
internal sealed record RoleGrant(ViewEffect View, IReadOnlyList<string> Permissions, bool IsSystem = false);
internal sealed record PermissionSnapshot(bool SessionValid, bool AccountActive, bool EmailVerified, bool ServerActive,
    bool ChannelActive, bool MembershipActive, bool IsOwner, string Kind, ViewEffect DefaultView,
    IReadOnlyList<RoleGrant> Roles, ViewEffect UserView);
internal sealed record PermissionEvaluation(bool CanView, bool CanSendText, IReadOnlyList<string> ManagementPermissions,
    bool CanManageExistingChannel);

internal static class PermissionEvaluator
{
    public static IReadOnlyList<string> Management(bool foundation, bool owner, IEnumerable<RoleGrant> roles) => !foundation
        ? [] : (owner ? ManagementPermissions.Codes : roles.Where(role => !role.IsSystem).SelectMany(role => role.Permissions)
            .Where(ManagementPermissions.Contains)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();

    public static PermissionEvaluation Evaluate(PermissionSnapshot input)
    {
        var foundation = input.SessionValid && input.AccountActive && input.EmailVerified && input.ServerActive && input.MembershipActive;
        var management = Management(foundation, input.IsOwner, input.Roles);
        var view = false;
        if (foundation && input.ChannelActive)
        {
            view = input.DefaultView == ViewEffect.Allow;
            if (input.Roles.Any(role => role.View == ViewEffect.Deny)) view = false;
            else if (input.Roles.Any(role => role.View == ViewEffect.Allow)) view = true;
            if (input.UserView != ViewEffect.Inherit) view = input.UserView == ViewEffect.Allow;
            if (input.IsOwner) view = true;
        }
        return new(view, view && input.Kind == "text", management, view && management.Contains("manage_channels"));
    }
}
