using SCDC.DbMigrator;

namespace SCDC.Api.Tests.Community;

public sealed partial class CommunityMigrationTests
{
    [Fact]
    public async Task Community_access_upgrade_preserves_business_rows_and_has_matching_bootstrap_checksum()
    {
        var bootstrap=await ReadAsync("database/postgres/schema.sql");
        await ExecuteAsync(bootstrap[..bootstrap.IndexOf("-- S2-07 bootstrap snapshot;",StringComparison.Ordinal)]);
        await ExecuteAsync(await ReadAsync("database/postgres/seed.sql"));
        var before=await BusinessSnapshotAsync();
        var sql=await ReadAsync("database/postgres/migrations/"+CommunityAccessMigration.FileName);
        Assert.Contains(sql,bootstrap);
        Assert.True(await CommunityAccessMigration.ApplyAsync(_connectionString,sql));
        Assert.False(await CommunityAccessMigration.ApplyAsync(_connectionString,sql));
        await Assert.ThrowsAsync<MigrationPreflightException>(()=>CommunityAccessMigration.ApplyAsync(_connectionString,sql+"\n-- drift"));
        var after=await BusinessSnapshotAsync();
        foreach(var (table,rows) in before.Where(t=>!t.Key.EndsWith(".access_fences",StringComparison.Ordinal)))Assert.Equal(rows,after[table]);
        Assert.Equal(0,await CrossFksAsync("community"));
        Assert.True(await BoundaryScalarAsync("SELECT count(*) FROM community.access_scopes")>0);
    }
}
