using System.Text.Json;
using SCDC.DbMigrator;

if (args.Length is < 1 or > 2)
{
    Console.Error.WriteLine("Usage: dotnet run --project tools/SCDC.DbMigrator -- <migration.sql> [reviewed-server-map.json]");
    return 2;
}
try
{
    var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Database")
        ?? throw new InvalidOperationException("ConnectionStrings__Database must be configured.");
    var search = Path.GetFileName(args[0]) == "002-community-search.sql";
    var roles = Path.GetFileName(args[0]) == "003-community-roles.sql";
    if ((search || roles) && args.Length != 1)
        throw new MigrationPreflightException("Only migration 001 accepts a visibility map.");
    var map = args.Length == 2
        ? JsonSerializer.Deserialize<Dictionary<Guid, LegacyServerSettings>>(await File.ReadAllTextAsync(args[1]),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!
        : new Dictionary<Guid, LegacyServerSettings>();
    var sql = await File.ReadAllTextAsync(args[0]);
    var applied = roles ? await CommunityRoleMigration.ApplyAsync(connection, sql)
        : search ? await CommunitySearchMigration.ApplyAsync(connection, sql)
        : await CommunityMigration.ApplyAsync(connection, sql, map);
    var version = roles ? "003" : search ? "002" : "001";
    Console.WriteLine(applied ? $"Community migration {version} applied." : $"Community migration {version} already applied with matching checksum.");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error is MigrationPreflightException ? error.Message : $"Migration failed: {error.GetType().Name}. No changes committed.");
    return 1;
}
