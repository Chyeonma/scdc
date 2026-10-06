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
    var map = args.Length == 2
        ? JsonSerializer.Deserialize<Dictionary<Guid, LegacyServerSettings>>(await File.ReadAllTextAsync(args[1]),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!
        : new Dictionary<Guid, LegacyServerSettings>();
    var applied = await CommunityMigration.ApplyAsync(connection, await File.ReadAllTextAsync(args[0]), map);
    Console.WriteLine(applied ? "Community migration 001 applied." : "Community migration 001 already applied with matching checksum.");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error is MigrationPreflightException ? error.Message : $"Migration failed: {error.GetType().Name}. No changes committed.");
    return 1;
}
