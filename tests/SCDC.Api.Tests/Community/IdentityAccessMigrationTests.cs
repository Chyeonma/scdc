using SCDC.DbMigrator;

namespace SCDC.Api.Tests.Community;

public sealed partial class CommunityMigrationTests
{
    [Fact]
    public async Task Identity_access_upgrade_preserves_business_rows_and_backfills_both_owner_floors()
    {
        var bootstrap = await ReadAsync("database/postgres/schema.sql");
        await ExecuteAsync(bootstrap[..bootstrap.IndexOf("-- S2-06 bootstrap snapshot;", StringComparison.Ordinal)]);
        await ExecuteAsync(await ReadAsync("database/postgres/seed.sql"));
        var before = await BusinessSnapshotAsync();
        var sql = await ReadAsync("database/postgres/migrations/" + IdentityAccessMigration.FileName);
        Assert.True(await IdentityAccessMigration.ApplyAsync(_connectionString, sql));
        Assert.False(await IdentityAccessMigration.ApplyAsync(_connectionString, sql));
        await Assert.ThrowsAsync<MigrationPreflightException>(() => IdentityAccessMigration.ApplyAsync(_connectionString, sql + "\n-- drift"));
        var after = await BusinessSnapshotAsync();
        foreach (var (table, rows) in before.Where(t => !t.Key.EndsWith(".access_fences", StringComparison.Ordinal)))
            Assert.Equal(rows, after[table]);
        var expected = await BoundaryScalarAsync("SELECT count(*) FROM identity.access_scopes");
        Assert.True(expected > 0);
        Assert.Equal(expected, await BoundaryScalarAsync("SELECT count(*) FROM community.access_fences WHERE authority=1"));
        Assert.Equal(expected, await BoundaryScalarAsync("SELECT count(*) FROM messaging.access_fences WHERE authority=1"));
        Assert.Equal(0, await CrossFksAsync("identity"));
    }
}
