using SCDC.BuildingBlocks.Infrastructure.Persistence;

namespace SCDC.Modules.Community.Infrastructure.Persistence;

internal static class ChannelAccessMaintenance
{
    // Server mutation lock must already be held. Called before removing overrides.
    public static async Task<bool> BumpAsync(RelationalWorkScope scope, Guid server, Guid target, bool role, CancellationToken ct)
    {
        var source = role ? "community.channel_role_overrides" : "community.channel_user_overrides";
        var column = role ? "role_id" : "user_id";
        var affected = $"c.server_id=@server AND EXISTS(SELECT 1 FROM {source} o WHERE o.space_id=c.space_id AND o.{column}=@target)";
        await using var check = scope.CreateCommand($"SELECT EXISTS(SELECT 1 FROM community.channels c WHERE {affected} AND(c.version=2147483647 OR c.access_version=2147483647))");
        check.Parameters.AddWithValue("server", server); check.Parameters.AddWithValue("target", target);
        if (await check.ExecuteScalarAsync(ct) is true) return false;
        await using var bump = scope.CreateCommand($"UPDATE community.channels c SET access_version=access_version+1 WHERE {affected}");
        bump.Parameters.AddWithValue("server", server); bump.Parameters.AddWithValue("target", target);
        await bump.ExecuteNonQueryAsync(ct); return true;
    }
}
