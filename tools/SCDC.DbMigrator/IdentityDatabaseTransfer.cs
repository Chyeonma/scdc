using System.Data;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Npgsql;

namespace SCDC.DbMigrator;

public sealed record IdentityTransferOptions(string Engine, string SourceContainer, string TargetContainer,
    string ArtifactDirectory, bool Quiesced, bool RollbackBeforeWrites = false);
public sealed record IdentityTableManifest(string Table, long Rows, string Sha256);
public sealed record IdentitySequenceManifest(string Sequence, long LastValue, bool IsCalled);
public sealed record IdentityDataManifest(string SchemaSha256, IdentityTableManifest[] Tables, IdentitySequenceManifest[] Sequences)
{
    public string Sha256 => IdentityDatabaseTransfer.Hash(JsonSerializer.Serialize(new { SchemaSha256, Tables, Sequences }));
}

// Offline, one-owner transfer. No source DROP, cross-database transaction or secret in argv.
public static class IdentityDatabaseTransfer
{
    public const long BaselineVersion = 2026112503;
    private const string RuntimeRole = "scdc_identity";
    private const string SourceEnv = "ConnectionStrings__TransferSource";
    private const string TargetEnv = "ConnectionStrings__TransferTarget";
    private sealed record TransferRecord(string SourceId, string SourceHash, string? ArchiveHash, string Status);

    public static async Task<bool> RunAsync(IdentityTransferOptions options, CancellationToken ct = default)
    {
        if (!options.Quiesced) throw new MigrationPreflightException("Stop all Identity writers and pass --quiesced.");
        if (options.Engine is not ("docker" or "podman") || !SafeContainer(options.SourceContainer) || !SafeContainer(options.TargetContainer))
            throw new MigrationPreflightException("Select docker/podman and explicit container names.");
        var evidence = Path.GetFullPath(options.ArtifactDirectory);
        if (Directory.Exists(evidence) || File.Exists(evidence))
            throw new MigrationPreflightException("Use a new private artifact directory for each attempt.");
        if (OperatingSystem.IsWindows()) Directory.CreateDirectory(evidence);
        else Directory.CreateDirectory(evidence, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

        var sourceSettings = Settings(SourceEnv);
        var targetSettings = Settings(TargetEnv);
        await using var source = new NpgsqlConnection(sourceSettings.ConnectionString);
        await using var target = new NpgsqlConnection(targetSettings.ConnectionString);
        await source.OpenAsync(ct); await target.OpenAsync(ct);
        await Execute(source, "SET TIME ZONE 'UTC'; SET datestyle='ISO,YMD'; SET lock_timeout='5s'", ct);
        await Execute(target, "SET TIME ZONE 'UTC'; SET datestyle='ISO,YMD'; SET lock_timeout='5s'", ct);
        var sourceId = await DatabaseId(source, ct);
        var targetId = await DatabaseId(target, ct);
        if (sourceId.Split(':')[0] == targetId.Split(':')[0])
            throw new MigrationPreflightException("Identity must move to a different PostgreSQL instance (D03).");
        await LockRunner(source, ct); await LockRunner(target, ct);
        await VerifyContainer(options, sourceSettings, options.SourceContainer, sourceId, evidence, "source", ct);
        await VerifyContainer(options, targetSettings, options.TargetContainer, targetId, evidence, "target", ct);
        await RequireSourceBaseline(source, ct);
        await RequireTargetBaseline(target, ct);
        var record = await ReadRecord(target, ct);
        if (record is not null && record.SourceId != sourceId)
            throw new MigrationPreflightException("Target belongs to a different source; no data was replaced.");
        await RequireRuntimeRole(source, ct); await RequireRuntimeRole(target, ct);
        await NoClients(source, ct);
        if (record is null)
        {
            await NoClients(target, ct);
            if (await HasBusinessRows(target, ct))
                throw new MigrationPreflightException("Target contains business data with no matching transfer record; no data was replaced.");
        }

        // NOLOGIN plus drained clients prevents an old container from reopening the source.
        // Failures intentionally leave the source frozen; rollback is a separate guarded mode.
        if (record?.Status == "ready" && await CanLogin(source, ct))
            throw new MigrationPreflightException("Ready target has a writable source; stop and investigate split writers.");
        await Execute(source, "ALTER ROLE scdc_identity NOLOGIN", ct);
        await NoClients(source, ct);
        await using var sourceLock = await source.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var tables = await TableNames(source, ct, sourceLock);
        await Execute(source, "LOCK TABLE " + string.Join(',', tables.Select(t => "identity." + Quote(t))) + " IN SHARE MODE", ct, sourceLock);
        var original = await Manifest(source, sourceSettings, options, options.SourceContainer, evidence, "source", ct, sourceLock);
        await WritePrivate(Path.Combine(evidence, "source.json"), JsonSerializer.Serialize(original, JsonOptions), ct);
        if (record is not null && record.SourceHash != original.Sha256)
            throw new MigrationPreflightException("Source changed since the recorded transfer; reconcile before proceeding.");

        if (options.RollbackBeforeWrites)
        {
            if (record is null) throw new MigrationPreflightException("No recorded transfer exists to roll back.");
            await NoClients(target, ct);
            var wasLogin = await CanLogin(target, ct);
            await Execute(target, "ALTER ROLE scdc_identity NOLOGIN", ct);
            try
            {
                await NoClients(target, ct);
                var copied = await HasIdentityTables(target, ct);
                if (copied)
                {
                    var current = await Manifest(target, targetSettings, options, options.TargetContainer, evidence, "rollback-target", ct);
                    if (current.Sha256 != original.Sha256)
                        throw new MigrationPreflightException("Rollback refused: target changed, including generation/sequence. Reconcile or fix forward.");
                }
                else if (record.Status == "ready")
                    throw new MigrationPreflightException("Ready target schema is missing; rollback requires review.");
                await Execute(target, "UPDATE scdc_migrations.identity_transfer SET status='rolled_back',updated_at=clock_timestamp() WHERE service='identity'", ct);
                await sourceLock.CommitAsync(ct);
                await Execute(source, "ALTER ROLE scdc_identity LOGIN", ct);
                await WritePrivate(Path.Combine(evidence, "result.json"), "{\"result\":\"source-restored-before-target-writes\"}\n", ct);
                return true;
            }
            catch
            {
                // A rejected rollback keeps the ready target usable; never reopens the source.
                if (wasLogin && record.Status == "ready") await Execute(target, "ALTER ROLE scdc_identity LOGIN", CancellationToken.None);
                throw;
            }
        }
        if (record?.Status == "ready")
        {
            // Target may now contain legitimate writes. Never restore over it on a repeat run.
            await sourceLock.CommitAsync(ct);
            await WritePrivate(Path.Combine(evidence, "result.json"), "{\"result\":\"already-ready-no-copy\"}\n", ct);
            return false;
        }

        await NoClients(target, ct);
        var populated = await HasBusinessRows(target, ct);
        if (populated && record is null)
            throw new MigrationPreflightException("Target contains business data with no matching transfer record; no data was replaced.");
        await Execute(target, "ALTER ROLE scdc_identity NOLOGIN", ct);
        await NoClients(target, ct);
        var archiveHash = record?.ArchiveHash;
        var restoreRequired = true;
        if (record is not null && await HasIdentityTables(target, ct))
        {
            var resumed = await Manifest(target, targetSettings, options, options.TargetContainer, evidence, "resume-target", ct);
            if (resumed.Sha256 == original.Sha256) restoreRequired = false;
            else if (populated)
                throw new MigrationPreflightException("Interrupted target differs from the source; keep writers frozen and review it.");
        }
        await using (var prepare = new NpgsqlCommand("""
            INSERT INTO scdc_migrations.identity_transfer(service,source_id,source_hash,status)
            VALUES('identity',@source,@hash,'copying') ON CONFLICT(service) DO UPDATE
            SET status='copying',updated_at=clock_timestamp()
            """, target))
        {
            prepare.Parameters.AddWithValue("source", sourceId); prepare.Parameters.AddWithValue("hash", original.Sha256);
            await prepare.ExecuteNonQueryAsync(ct);
        }
        if (restoreRequired)
        {
            var archive = Path.Combine(evidence, "identity.dump");
            await using (var output = PrivateFile(archive))
                await Pg(options, sourceSettings, options.SourceContainer, "pg_dump",
                    ["--format=custom", "--schema=identity", "--no-owner", "--no-privileges"], evidence, "dump", ct, output: output);
            await using (var file = File.OpenRead(archive)) archiveHash = Convert.ToHexStringLower(await SHA256.HashDataAsync(file, ct));
            await using (var checkpoint = new NpgsqlCommand("UPDATE scdc_migrations.identity_transfer SET archive_hash=@archive WHERE service='identity' AND status='copying'", target))
            {
                checkpoint.Parameters.AddWithValue("archive", archiveHash);
                await checkpoint.ExecuteNonQueryAsync(ct);
            }
            // Only an empty, verified target or an atomic-restore retry reaches this DROP.
            await Execute(target, "DROP SCHEMA IF EXISTS identity CASCADE", ct);
            await using var input = File.OpenRead(archive);
            await Pg(options, targetSettings, options.TargetContainer, "pg_restore",
                ["--no-owner", "--no-privileges", "--single-transaction", "--exit-on-error"], evidence, "restore", ct, input: input);
        }
        var restored = await Manifest(target, targetSettings, options, options.TargetContainer, evidence, "target", ct);
        await WritePrivate(Path.Combine(evidence, "target.json"), JsonSerializer.Serialize(restored, JsonOptions), ct);
        if (original.Sha256 != restored.Sha256)
            throw new MigrationPreflightException("Source and target manifests differ; writers remain frozen.");
        await using (var commit = await target.BeginTransactionAsync(ct))
        {
            await Execute(target, await File.ReadAllTextAsync("database/postgres/identity/runtime-grants.sql", ct), ct, commit);
            await using var ready = new NpgsqlCommand("""
                UPDATE scdc_migrations.identity_transfer SET status='ready',archive_hash=@archive,updated_at=clock_timestamp() WHERE service='identity';
                UPDATE scdc_migrations.service_baselines SET origin='transfer' WHERE service='identity';
                ALTER ROLE scdc_identity LOGIN;
                """, target, commit);
            ready.Parameters.AddWithValue("archive", (object?)archiveHash ?? DBNull.Value);
            await ready.ExecuteNonQueryAsync(ct);
            await commit.CommitAsync(ct);
        }
        await sourceLock.CommitAsync(ct);
        await WritePrivate(Path.Combine(evidence, "result.json"), JsonSerializer.Serialize(new
        {
            result = "ready", source = sourceId, target = targetId, dataSha256 = original.Sha256,
            tables = original.Tables.Length, rows = original.Tables.Sum(t => t.Rows),
            sequences = original.Sequences.Length, archiveSha256 = archiveHash,
            sourceRuntimeLogin = false, targetRuntimeLogin = true
        }, JsonOptions), ct);
        return true;
    }

    public static string Hash(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static string Quote(string name) => new NpgsqlCommandBuilder().QuoteIdentifier(name);
    private static bool SafeContainer(string value) => value.Length is > 0 and < 128 && char.IsLetterOrDigit(value[0])
        && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');
    private static NpgsqlConnectionStringBuilder Settings(string variable)
    {
        var value = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrWhiteSpace(value)) throw new MigrationPreflightException(variable + " must be configured.");
        return new(value) { Pooling = false, IncludeErrorDetail = false, ApplicationName = "SCDC.IdentityTransfer", Timeout = 5, CommandTimeout = 60 };
    }
    private static async Task Execute(NpgsqlConnection db, string sql, CancellationToken ct, NpgsqlTransaction? tx = null)
    { await using var cmd = new NpgsqlCommand(sql, db, tx); await cmd.ExecuteNonQueryAsync(ct); }
    private static async Task<object?> Scalar(NpgsqlConnection db, string sql, CancellationToken ct, NpgsqlTransaction? tx = null)
    { await using var cmd = new NpgsqlCommand(sql, db, tx); return await cmd.ExecuteScalarAsync(ct); }
    private static async Task LockRunner(NpgsqlConnection db, CancellationToken ct)
    {
        if (await Scalar(db, "SELECT pg_try_advisory_lock(1935893324,303)", ct) is not true)
            throw new MigrationPreflightException("Another Identity transfer owns the migration lock.");
    }
    private static async Task<string> DatabaseId(NpgsqlConnection db, CancellationToken ct)
    {
        if (Convert.ToInt32(await Scalar(db, "SHOW server_version_num", ct)) / 10000 != 18)
            throw new MigrationPreflightException("Use PostgreSQL 18 for both endpoints.");
        return (string)(await Scalar(db, "SELECT system_identifier::text||':'||(SELECT oid::text FROM pg_database WHERE datname=current_database())||':'||current_database() FROM pg_control_system()", ct))!;
    }
    private static async Task NoClients(NpgsqlConnection db, CancellationToken ct)
    {
        if (Convert.ToInt64(await Scalar(db, "SELECT count(*) FROM pg_stat_activity WHERE datname=current_database() AND backend_type='client backend' AND pid<>pg_backend_pid()", ct)) != 0)
            throw new MigrationPreflightException("Database has other clients; drain writers/connections first.");
    }
    private static async Task RequireRuntimeRole(NpgsqlConnection db, CancellationToken ct)
    {
        if (await Scalar(db, "SELECT NOT(rolsuper OR rolcreatedb OR rolcreaterole OR rolreplication OR rolbypassrls) FROM pg_roles WHERE rolname='scdc_identity'", ct) is not true)
            throw new MigrationPreflightException("Expected a restricted scdc_identity runtime role.");
        if (await Scalar(db, "SELECT count(*)=0 FROM pg_auth_members WHERE member=(SELECT oid FROM pg_roles WHERE rolname='scdc_identity')", ct) is not true)
            throw new MigrationPreflightException("Runtime role must not inherit another role.");
    }
    private static async Task<bool> CanLogin(NpgsqlConnection db, CancellationToken ct) =>
        await Scalar(db, "SELECT rolcanlogin FROM pg_roles WHERE rolname='scdc_identity'", ct) is true;
    private static async Task RequireSourceBaseline(NpgsqlConnection db, CancellationToken ct)
    {
        foreach (var file in new[] { "20261120_s2-03_module_databases.sql", "20261123_s2-06_identity_access.sql" })
        {
            var version = file.Contains("s2-03", StringComparison.Ordinal) ? 20261120 : 2026112306;
            await using var cmd = new NpgsqlCommand("SELECT checksum FROM identity.schema_migrations WHERE module='identity' AND version=@version", db);
            cmd.Parameters.AddWithValue("version", version);
            if (await cmd.ExecuteScalarAsync(ct) as string != Hash(await File.ReadAllTextAsync("database/postgres/migrations/" + file, ct)))
                throw new MigrationPreflightException("Source needs matching S2-03/S2-06 Identity migration checksums.");
        }
    }
    private static async Task RequireTargetBaseline(NpgsqlConnection db, CancellationToken ct)
    {
        if (await Scalar(db, "SELECT to_regclass('scdc_migrations.service_baselines') IS NOT NULL AND to_regclass('scdc_migrations.identity_transfer') IS NOT NULL", ct) is not true)
            throw new MigrationPreflightException("Initialize a dedicated Identity target baseline first.");
        if (Convert.ToInt64(await Scalar(db, "SELECT count(*) FROM pg_namespace WHERE nspname !~ '^pg_' AND nspname NOT IN ('information_schema','public','identity','scdc_migrations')", ct)) != 0)
            throw new MigrationPreflightException("Target contains schemas outside Identity ownership.");
        await using var cmd = new NpgsqlCommand("SELECT checksum FROM scdc_migrations.service_baselines WHERE service='identity' AND version=@version", db);
        cmd.Parameters.AddWithValue("version", BaselineVersion);
        if (await cmd.ExecuteScalarAsync(ct) as string != Hash(await File.ReadAllTextAsync("database/postgres/identity/schema.sql", ct)))
            throw new MigrationPreflightException("Identity target baseline checksum differs from this tool.");
    }
    private static async Task<TransferRecord?> ReadRecord(NpgsqlConnection db, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand("SELECT source_id,source_hash,archive_hash,status FROM scdc_migrations.identity_transfer WHERE service='identity'", db);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? new(reader.GetString(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2), reader.GetString(3)) : null;
    }
    private static async Task<string[]> TableNames(NpgsqlConnection db, CancellationToken ct, NpgsqlTransaction? tx = null)
    {
        var names = new List<string>();
        await using var cmd = new NpgsqlCommand("SELECT tablename FROM pg_tables WHERE schemaname='identity' ORDER BY tablename COLLATE \"C\"", db, tx);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) names.Add(reader.GetString(0));
        return names.ToArray();
    }
    private static async Task<bool> HasIdentityTables(NpgsqlConnection db, CancellationToken ct) =>
        (await TableNames(db, ct)).Length != 0;
    private static async Task<bool> HasBusinessRows(NpgsqlConnection db, CancellationToken ct)
    {
        foreach (var name in await TableNames(db, ct))
        {
            // The fresh baseline has no MVP migration claims; every table must be empty.
            if (await Scalar(db, "SELECT EXISTS(SELECT 1 FROM identity." + Quote(name) + ")", ct) is true) return true;
        }
        return false;
    }
    private static async Task<IdentityDataManifest> Manifest(NpgsqlConnection db, NpgsqlConnectionStringBuilder settings,
        IdentityTransferOptions options, string container, string evidence, string label, CancellationToken ct, NpgsqlTransaction? tx = null)
    {
        var schema = await Pg(options, settings, container, "pg_dump", ["--schema=identity", "--schema-only", "--no-owner", "--no-privileges", "--no-comments"], evidence, label + "-schema", ct);
        await WritePrivate(Path.Combine(evidence, label + ".schema.sql"), schema, ct);
        schema = string.Join('\n', schema.Split('\n').Where(line => !line.StartsWith("--", StringComparison.Ordinal) && !line.StartsWith('\\') && !string.IsNullOrWhiteSpace(line)));
        // PG18 flattens this known MVP CHECK's associative AND when parsing the dump.
        // Normalize this exact, reviewed expression only; do not strip arbitrary parentheses.
        schema = schema.Replace(
            "CONSTRAINT ck_user_emails_format CHECK ((((char_length((email)::text) >= 3) AND (char_length((email)::text) <= 254)) AND (POSITION(('@'::text) IN (email)) > 1)))",
            "CONSTRAINT ck_user_emails_format CHECK (((char_length((email)::text) >= 3) AND (char_length((email)::text) <= 254) AND (POSITION(('@'::text) IN (email)) > 1)))", StringComparison.Ordinal);
        var tables = new List<IdentityTableManifest>();
        foreach (var table in await TableNames(db, ct, tx))
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            await using var command = new NpgsqlCommand("SELECT to_jsonb(t)::text FROM identity." + Quote(table) + " t ORDER BY to_jsonb(t)::text COLLATE \"C\"", db, tx);
            await using var rows = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, ct);
            long count = 0;
            while (await rows.ReadAsync(ct)) { hash.AppendData(Encoding.UTF8.GetBytes(rows.GetString(0) + "\n")); count++; }
            tables.Add(new(table, count, Convert.ToHexStringLower(hash.GetHashAndReset())));
        }
        var sequenceNames = new List<string>();
        await using (var command = new NpgsqlCommand("SELECT sequencename FROM pg_sequences WHERE schemaname='identity' ORDER BY sequencename COLLATE \"C\"", db, tx))
        await using (var rows = await command.ExecuteReaderAsync(ct))
            while (await rows.ReadAsync(ct)) sequenceNames.Add(rows.GetString(0));
        var sequences = new List<IdentitySequenceManifest>();
        foreach (var name in sequenceNames)
        {
            await using var command = new NpgsqlCommand("SELECT last_value,is_called FROM identity." + Quote(name), db, tx);
            await using var rows = await command.ExecuteReaderAsync(ct); await rows.ReadAsync(ct);
            sequences.Add(new(name, rows.GetInt64(0), rows.GetBoolean(1)));
        }
        return new(Hash(schema), tables.ToArray(), sequences.ToArray());
    }
    private static async Task VerifyContainer(IdentityTransferOptions options, NpgsqlConnectionStringBuilder settings,
        string container, string expected, string evidence, string label, CancellationToken ct)
    {
        var actual = await Pg(options, settings, container, "psql", ["-X", "-A", "-t", "-v", "ON_ERROR_STOP=1", "-c",
            "SELECT system_identifier::text||':'||(SELECT oid::text FROM pg_database WHERE datname=current_database())||':'||current_database() FROM pg_control_system()"], evidence, label + "-endpoint", ct);
        if (actual.Trim() != expected) throw new MigrationPreflightException("Container and configured database endpoint differ.");
    }
    private static async Task<string> Pg(IdentityTransferOptions options, NpgsqlConnectionStringBuilder settings,
        string container, string program, string[] arguments, string evidence, string label, CancellationToken ct, Stream? input = null, Stream? output = null)
    {
        var start = new ProcessStartInfo(options.Engine) { RedirectStandardInput = input is not null, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var item in new[] { "exec", "-i", "--env", "PGPASSWORD", "--env", "PGOPTIONS", container, program, "--host=127.0.0.1", "--port=5432", "--username=" + settings.Username, "--dbname=" + settings.Database }.Concat(arguments)) start.ArgumentList.Add(item);
        start.Environment["PGPASSWORD"] = settings.Password ?? "";
        start.Environment["PGOPTIONS"] = "-c timezone=UTC -c datestyle=ISO,YMD";
        using var child = Process.Start(start) ?? throw new MigrationPreflightException("Cannot start PostgreSQL client.");
        using var kill = ct.Register(() => { try { child.Kill(entireProcessTree: true); } catch (InvalidOperationException) { } });
        var errors = child.StandardError.ReadToEndAsync(ct);
        var text = output is null ? child.StandardOutput.ReadToEndAsync(ct) : null;
        var copyOut = output is null ? Task.CompletedTask : child.StandardOutput.BaseStream.CopyToAsync(output, ct);
        var copyIn = input is null ? Task.CompletedTask : SendInput(child, input, ct);
        await Task.WhenAll(copyIn, copyOut, child.WaitForExitAsync(ct));
        await WritePrivate(Path.Combine(evidence, label + ".log"), await errors, ct);
        if (child.ExitCode != 0) throw new MigrationPreflightException(program + " failed; inspect the private transfer diagnostics.");
        return text is null ? "" : await text;
    }
    private static async Task SendInput(Process child, Stream input, CancellationToken ct)
    { await input.CopyToAsync(child.StandardInput.BaseStream, ct); child.StandardInput.Close(); }
    private static FileStream PrivateFile(string path)
    {
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        return new(path, options);
    }
    private static async Task WritePrivate(string path, string text, CancellationToken ct)
    { await using var stream = PrivateFile(path); await stream.WriteAsync(Encoding.UTF8.GetBytes(text), ct); }
}
