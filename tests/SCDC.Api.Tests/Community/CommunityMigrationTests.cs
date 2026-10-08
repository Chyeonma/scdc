using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SCDC.Api.Tests.Infrastructure;
using SCDC.DbMigrator;

namespace SCDC.Api.Tests.Community;

public sealed partial class CommunityMigrationTests : IAsyncLifetime
{
    private readonly string _database = $"scdc_migration_{Guid.NewGuid():N}_test";
    private string _admin = null!;
    private string _connectionString = null!;
    public static string RepoRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SCDC.slnx"))) directory = directory.Parent;
            return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
        }
    }
    private static Task<string> ReadAsync(string path) => File.ReadAllTextAsync(Path.Combine(RepoRoot, path));

    public async Task InitializeAsync()
    {
        await using var factory = new SCDCWebApplicationFactory();
        _admin = factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("Database")!;
        Assert.EndsWith("_test", new NpgsqlConnectionStringBuilder(_admin).Database);
        await using var connection = new NpgsqlConnection(_admin);
        await connection.OpenAsync();
        await using var create = new NpgsqlCommand($"CREATE DATABASE {_database}", connection);
        await create.ExecuteNonQueryAsync();
        _connectionString = new NpgsqlConnectionStringBuilder(_admin) { Database = _database }.ConnectionString;
    }

    private async Task ExecuteAsync(string sql)
    {
        // Bootstrap fixtures also run through psql; its commands are not SQL.
        sql = string.Join('\n', sql.Split('\n').Where(line => !line.TrimStart().StartsWith('\\')));
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = 30 };
        await command.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task Migration_is_repeatable_and_rejects_modified_checksum()
    {
        await ExecuteAsync(await ReadAsync("tests/SCDC.Api.Tests/Community/Fixtures/legacy-schema.sql"));
        var sql = await ReadAsync("database/postgres/migrations/001-community-create-view.sql");
        Assert.True(await CommunityMigration.ApplyAsync(_connectionString, sql));
        Assert.False(await CommunityMigration.ApplyAsync(_connectionString, sql));
        await Assert.ThrowsAsync<MigrationPreflightException>(() => CommunityMigration.ApplyAsync(_connectionString, sql + "\n-- changed"));
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var units = new NpgsqlCommand("SELECT common.utf16_length('👩‍💻'),common.utf16_length(''),common.utf16_length('a')", connection);
        await using var reader = await units.ExecuteReaderAsync();
        await reader.ReadAsync();
        Assert.Equal(5, reader.GetInt32(0));
        Assert.Equal(0, reader.GetInt32(1));
        Assert.Equal(1, reader.GetInt32(2));
    }

    [Fact]
    public async Task Legacy_mapping_is_explicit_and_preserves_version_and_membership()
    {
        await ExecuteAsync(await ReadAsync("tests/SCDC.Api.Tests/Community/Fixtures/legacy-schema.sql"));
        var user = Guid.CreateVersion7();
        var server = Guid.CreateVersion7();
        await ExecuteAsync($"""
            INSERT INTO identity.users(id,username,status) VALUES('{user}','legacy_user',1);
            INSERT INTO community.servers(id,owner_user_id,name,slug) VALUES('{server}','{user}','Legacy','legacy-server');
            INSERT INTO community.server_members(server_id,user_id) VALUES('{server}','{user}');
            INSERT INTO community.roles(server_id,name,is_default,is_system) VALUES('{server}','@everyone',true,true);
            """);
        var sql = await ReadAsync("database/postgres/migrations/001-community-create-view.sql");
        await Assert.ThrowsAsync<MigrationPreflightException>(() => CommunityMigration.ApplyAsync(_connectionString, sql));
        Assert.True(await CommunityMigration.ApplyAsync(_connectionString, sql,
            new Dictionary<Guid,LegacyServerSettings> { [server] = new("private","immediate") }));
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var query = new NpgsqlCommand("SELECT s.visibility,s.version,m.membership_id,m.status FROM community.servers s JOIN community.server_members m ON m.server_id=s.id", connection);
        await using var reader = await query.ExecuteReaderAsync();
        await reader.ReadAsync();
        Assert.Equal(2,reader.GetInt16(0));
        Assert.Equal(1,reader.GetInt32(1));
        Assert.NotEqual(Guid.Empty,reader.GetGuid(2));
        Assert.Equal(1,reader.GetInt16(3));
    }

    [Fact]
    public async Task Preflight_failure_leaves_legacy_schema_and_rows_untouched()
    {
        await ExecuteAsync(await ReadAsync("tests/SCDC.Api.Tests/Community/Fixtures/legacy-schema.sql"));
        var user=Guid.CreateVersion7(); var server=Guid.CreateVersion7();
        await ExecuteAsync($"""
            INSERT INTO identity.users(id,username,status) VALUES('{user}','broken_owner',1);
            INSERT INTO community.servers(id,owner_user_id,name,slug) VALUES('{server}','{user}','Legacy','legacy-server');
            """);
        var sql=await ReadAsync("database/postgres/migrations/001-community-create-view.sql");
        var failure=await Assert.ThrowsAsync<MigrationPreflightException>(() => CommunityMigration.ApplyAsync(_connectionString,sql,
            new Dictionary<Guid,LegacyServerSettings>{[server]=new("public","immediate")}));
        Assert.Contains("owner membership",failure.Message);
        await using var connection=new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var query=new NpgsqlCommand("SELECT count(*) FROM information_schema.columns WHERE table_schema='community' AND table_name='servers' AND column_name='visibility'",connection);
        Assert.Equal(0L,await query.ExecuteScalarAsync());
        await using var rows=new NpgsqlCommand("SELECT count(*) FROM community.servers",connection);
        Assert.Equal(1L,await rows.ExecuteScalarAsync());
    }

    [Fact]
    public async Task New_bootstrap_seed_and_migration_ledger_are_compatible()
    {
        await ExecuteAsync(await ReadAsync("database/postgres/schema.sql"));
        await ExecuteAsync(await ReadAsync("database/postgres/seed.sql"));
        Assert.False(await CommunityMigration.ApplyAsync(_connectionString,await ReadAsync("database/postgres/migrations/001-community-create-view.sql")));
        var id=Guid.CreateVersion7();
        var failure=await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync($"""
            BEGIN;
            INSERT INTO community.servers(id,owner_user_id,name,slug,search_name) SELECT '{id}',id,'😀','{id:N}','😀' FROM identity.users LIMIT 1;
            INSERT INTO community.server_members(server_id,user_id) SELECT id,owner_user_id FROM community.servers WHERE id='{id}';
            COMMIT;
            """));
        Assert.Equal("23514",failure.SqlState);
    }

    public async Task DisposeAsync()
    {
        NpgsqlConnection.ClearPool(new NpgsqlConnection(_connectionString));
        await using var connection=new NpgsqlConnection(_admin);
        await connection.OpenAsync();
        await using var drop=new NpgsqlCommand($"DROP DATABASE IF EXISTS {_database} WITH (FORCE)",connection);
        await drop.ExecuteNonQueryAsync();
    }
}
