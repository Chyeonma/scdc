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
    if (search && args.Length != 1)
        throw new MigrationPreflightException("Migration 002 computes search keys; a visibility map is only used by migration 001.");
    var map = args.Length == 2
        ? JsonSerializer.Deserialize<Dictionary<Guid, LegacyServerSettings>>(await File.ReadAllTextAsync(args[1]),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!
        : new Dictionary<Guid, LegacyServerSettings>();
    var sql = await File.ReadAllTextAsync(args[0]);
    var applied = search ? await CommunitySearchMigration.ApplyAsync(connection, sql)
        : await CommunityMigration.ApplyAsync(connection, sql, map);
    var version = search ? "002" : "001";
    Console.WriteLine(applied ? $"Community migration {version} applied." : $"Community migration {version} already applied with matching checksum.");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error is MigrationPreflightException ? error.Message : $"Migration failed: {error.GetType().Name}. No changes committed.");
    return 1;
}
