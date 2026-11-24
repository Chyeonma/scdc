using SCDC.DbMigrator;

namespace SCDC.Api.Tests.Community;

public sealed partial class CommunityMigrationTests
{
    [Fact]
    public async Task Provision_upgrade_preserves_existing_data_mapping_checksums_and_rejects_drift()
    {
        var bootstrap = await ReadAsync("database/postgres/schema.sql");
        await ExecuteAsync(bootstrap[..bootstrap.IndexOf("-- S2-09 bootstrap snapshot;", StringComparison.Ordinal)] + "COMMIT;");
        await ExecuteAsync(await ReadAsync("database/postgres/seed.sql"));
        var before = await BusinessSnapshotAsync();
        var spaces = await SnapshotAsync("messaging.spaces");
        var sql = await ReadAsync("database/postgres/migrations/" + ChannelProvisionMigration.FileName);
        Assert.Contains(sql, bootstrap);
        Assert.True(await ChannelProvisionMigration.ApplyAsync(_connectionString, sql));
        Assert.False(await ChannelProvisionMigration.ApplyAsync(_connectionString, sql));
        await Assert.ThrowsAsync<MigrationPreflightException>(() => ChannelProvisionMigration.ApplyAsync(_connectionString, sql + "\n-- drift"));
        var after = await BusinessSnapshotAsync();
        foreach (var (table, rows) in before.Where(t => t.Key != "messaging.spaces")) Assert.Equal(rows, after[table]);
        Assert.Equal(spaces, await SnapshotAsync("(SELECT id,space_type,status,created_by_user_id,last_message_id,last_message_sequence,last_activity_at,created_at,updated_at,archived_at,deleted_at,version FROM messaging.spaces)"));
        Assert.Equal(await BoundaryScalarAsync("SELECT count(*) FROM community.channels WHERE kind=1"), await BoundaryScalarAsync("SELECT count(*) FROM community.channel_provisions WHERE phase=1 AND NOT delivery_pending"));
        Assert.Equal(await BoundaryScalarAsync("SELECT count(*) FROM community.channel_provisions"), await BoundaryScalarAsync("SELECT count(*) FROM messaging.channel_provisions WHERE phase=1"));
    }
}
