using Npgsql;
using SCDC.DbMigrator;

namespace SCDC.Api.Tests.Infrastructure;

internal static class IdentityEmailTestDatabase
{
    internal static async Task UpgradeAsync(string adminConnection)
    {
        if (!new NpgsqlConnectionStringBuilder(adminConnection).Database!.Contains("test", StringComparison.Ordinal))
            throw new InvalidOperationException("An isolated test database is required.");
        var sql = await File.ReadAllTextAsync(Path.Combine(
            SCDC.Api.Tests.Community.CommunityMigrationTests.RepoRoot, "database/postgres/identity", IdentityEmailMigration.FileName));
        await IdentityEmailMigration.ApplyAsync(adminConnection, sql);
    }
}
