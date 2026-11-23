using System.Text.Json;
using SCDC.DbMigrator;

if (args.Length is < 1 or > 2)
{
    Console.Error.WriteLine("Usage: dotnet run --project tools/SCDC.DbMigrator -- <migration.sql> [reviewed-map.json]");
    return 2;
}
try
{
    // This is an administrator/migrator connection, never a runtime module login.
    var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Migration")
        ?? throw new InvalidOperationException("ConnectionStrings__Migration must be configured.");
    if (Path.GetFileName(args[0]) == IdentityAccessMigration.FileName)
    {
        if (args.Length != 1) throw new MigrationPreflightException("S2-06 does not accept a data map.");
        var changed = await IdentityAccessMigration.ApplyAsync(connection, await File.ReadAllTextAsync(args[0]));
        Console.WriteLine(changed ? "S2-06 applied." : "S2-06 already applied with matching checksum.");
        return 0;
    }
    if (Path.GetFileName(args[0]) is SchemaBoundaryMigration.CommunityFile or SchemaBoundaryMigration.MessagingFile)
    {
        if (args.Length != 1)
            throw new MigrationPreflightException("S2-04/S2-05 do not accept a data map.");
        var changed = await SchemaBoundaryMigration.ApplyAsync(connection, args[0], await File.ReadAllTextAsync(args[0]));
        Console.WriteLine(changed ? "Schema-boundary migration applied." : "Schema-boundary migration already applied with matching checksum.");
        return 0;
    }
    if (Path.GetFileName(args[0]) == "20261120_s2-03_module_databases.sql")
    {
        var inboxMap = args.Length == 2
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync(args[1]))
            : null;
        var changed = await ModuleDatabaseMigration.ApplyAsync(connection, await File.ReadAllTextAsync(args[0]), inboxMap);
        Console.WriteLine(changed ? "S2-03 applied to Identity, Community and Messaging." : "S2-03 already applied with matching checksums.");
        return 0;
    }
    var search = Path.GetFileName(args[0]) == "002-community-search.sql";
    var roles = Path.GetFileName(args[0]) == "003-community-roles.sql";
    var channels = Path.GetFileName(args[0]) == "004-community-channels-access.sql";
    if ((search || roles) && args.Length != 1)
        throw new MigrationPreflightException("Only migration 001 accepts a visibility map.");
    var map = args.Length == 2 && !channels
        ? JsonSerializer.Deserialize<Dictionary<Guid, LegacyServerSettings>>(await File.ReadAllTextAsync(args[1]),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!
        : new Dictionary<Guid, LegacyServerSettings>();
    var sql = await File.ReadAllTextAsync(args[0]);
    var channelMap = channels && args.Length == 2
        ? JsonSerializer.Deserialize<Dictionary<Guid, LegacyChannelSettings>>(await File.ReadAllTextAsync(args[1]),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!
        : new Dictionary<Guid, LegacyChannelSettings>();
    if (Path.GetFileName(args[0]) != "001-community-create-view.sql" && !search && !roles && !channels)
        throw new MigrationPreflightException("Unsupported migration filename.");
    var applied = channels ? await CommunityChannelMigration.ApplyAsync(connection, sql, channelMap)
        : roles ? await CommunityRoleMigration.ApplyAsync(connection, sql)
        : search ? await CommunitySearchMigration.ApplyAsync(connection, sql)
        : await CommunityMigration.ApplyAsync(connection, sql, map);
    var version = channels ? "004" : roles ? "003" : search ? "002" : "001";
    Console.WriteLine(applied ? $"Community migration {version} applied." : $"Community migration {version} already applied with matching checksum.");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error is MigrationPreflightException ? error.Message : $"Migration failed: {error.GetType().Name}. No changes committed.");
    return 1;
}
