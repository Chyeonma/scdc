using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.BuildingBlocks.Application.Text;
using SCDC.BuildingBlocks.Infrastructure.Outbox;
using SCDC.BuildingBlocks.Infrastructure.Persistence;
using SCDC.Contracts.Identity;
using SCDC.Modules.Community.Features.Servers.Domain;
using SCDC.Modules.Community.Features.Memberships.Domain;
using SCDC.Modules.Community.Features.Permissions.Domain;
using SCDC.Modules.Community.Infrastructure;
using SCDC.Modules.Community.Infrastructure.Idempotency;
using SCDC.Modules.Community.Infrastructure.Paging;
using SCDC.Modules.Community.Infrastructure.Persistence;

namespace SCDC.Modules.Community.Features.Servers.Application;

internal sealed class ServerService(RelationalWorkScopeFactory scopes, IAccountAccessGuard guard,
    IOptionsMonitor<CommunityOptions> options, ServerReader reader, ServerCursorCodec cursors, SearchCursorCodec searchCursors,
    TransactionalOutbox outbox, TimeProvider clock, ILogger<ServerService> logger) : IServerService
{
    private sealed class RequestFailure(Error error) : Exception
    {
        public Error Error { get; } = error;
    }
    private sealed record NormalizedCreate(Guid Operation, string Name, string? Description, short Visibility);
    private static RequestFailure NotFound() => new(Error.NotFound("RESOURCE_NOT_FOUND", "Resource was not found."));

    public Task<Result<CreateServerResult>> CreateAsync(AccountActor actor, CreateServerCommand command, CancellationToken ct) => ExecuteAsync(async () =>
    {
        var data = Normalize(command);
        try
        {
            return await CreateOnceAsync(actor, data, ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505", ConstraintName: "pk_community_operations" })
        {
            // CreateOnce has disposed the whole failed context and transaction before this new scope.
            await using var scope = await scopes.OpenAsync(ct);
            var lease = await CheckAsync(actor, scope, ct);
            var operation = await FindOperationAsync(scope, actor.UserId, data.Operation, ct)
                ?? throw new InvalidOperationException("Committed operation winner was not found.");
            var result = await ReplayAsync(scope, actor, data, operation, ct);
            EnsureLease(lease);
            await scope.CommitAsync(ct);
            return result;
        }
    });

    private async Task<CreateServerResult> CreateOnceAsync(AccountActor actor, NormalizedCreate data, CancellationToken ct)
    {
        await using var scope = await scopes.OpenAsync(ct);
        var lease = await CheckAsync(actor, scope, ct);
        var existing = await FindOperationAsync(scope, actor.UserId, data.Operation, ct);
        if (existing is not null)
        {
            var replay = await ReplayAsync(scope, actor, data, existing, ct);
            EnsureLease(lease);
            await scope.CommitAsync(ct);
            return replay;
        }
        var keys = options.CurrentValue.Operations;
        var keyId = keys.ActiveKeyId;
        var hash = OperationFingerprint.Compute(keys.GetKey(keyId), actor.UserId, data.Operation, data.Name, data.Description, data.Visibility);
        var id = Guid.CreateVersion7();
        var now = clock.GetUtcNow();
        var contextOptions = new DbContextOptionsBuilder<CommunityDbContext>().UseNpgsql(scope.Connection, b => b.CommandTimeout(5)).Options;
        await using var db = new CommunityDbContext(contextOptions);
        await db.Database.UseTransactionAsync(scope.Transaction, ct);
        db.Add(new Server
        {
            Id = id,
            OwnerUserId = actor.UserId,
            Name = data.Name,
            SearchName = UnicodeTextPolicy.NormalizeNameKey(data.Name),
            Slug = id.ToString("N"),
            Description = data.Description,
            Visibility = data.Visibility,
            CreatedAt = now,
            UpdatedAt = now
        });
        db.Add(new Membership { ServerId = id, UserId = actor.UserId, MembershipId = Guid.CreateVersion7(), JoinedAt = now });
        db.Add(new Role { Id = Guid.CreateVersion7(), ServerId = id, CreatedAt = now, UpdatedAt = now });
        db.Add(new CommunityOperation { ActorUserId = actor.UserId, ClientOperationId = data.Operation, KeyId = keyId, Fingerprint = hash, ResourceId = id, CreatedAt = now });
        await db.SaveChangesAsync(ct);
        await outbox.AppendAsync(scope, "Community.ServerCreated.v1", "community.server", id, 1,
            new
            {
                serverId = id,
                ownerUserId = actor.UserId,
                accessVersion = "1"
            }, ct);
        var result = await reader.GetAsync(scope, actor.UserId, id, ct) as ServerDetail
            ?? throw new InvalidOperationException("Created server detail was not found.");
        EnsureLease(lease);
        await scope.CommitAsync(ct);
        return new(result, true);
    }
    private async Task<CreateServerResult> ReplayAsync(RelationalWorkScope scope, AccountActor actor, NormalizedCreate data,
        CommunityOperation operation, CancellationToken ct)
    {
        var key = options.CurrentValue.Operations.GetKey(operation.KeyId);
        var hash = OperationFingerprint.Compute(key, actor.UserId, data.Operation, data.Name, data.Description, data.Visibility, operation.FingerprintVersion);
        if (!CryptographicOperations.FixedTimeEquals(hash, operation.Fingerprint))
            throw new RequestFailure(Error.Conflict("OPERATION_CONFLICT", "Operation key was used with different data."));
        var view = await reader.GetAsync(scope, actor.UserId, operation.ResourceId, ct, requireExisting: true) ?? throw NotFound();
        if (view is not ServerDetail detail)
            throw new RequestFailure(Error.Forbidden("PERMISSION_DENIED", "Active membership is required to read the operation result."));
        return new(detail, false);
    }
    private static async Task<CommunityOperation?> FindOperationAsync(RelationalWorkScope scope, Guid actor, Guid operation, CancellationToken ct)
    {
        await using var query = scope.CreateCommand("""
            SELECT fingerprint_version,key_id,fingerprint,resource_id,created_at FROM community.operations
            WHERE actor_user_id=@actor AND kind='create_server' AND scope_id='00000000-0000-0000-0000-000000000000' AND client_operation_id=@operation
            """);
        query.Parameters.AddWithValue("actor", actor);
        query.Parameters.AddWithValue("operation", operation);
        await using var rows = await query.ExecuteReaderAsync(ct);
        return await rows.ReadAsync(ct) ? new CommunityOperation
        {
            ActorUserId = actor,
            ClientOperationId = operation,
            FingerprintVersion = rows.GetInt16(0),
            KeyId = rows.GetString(1),
            Fingerprint = rows.GetFieldValue<byte[]>(2),
            ResourceId = rows.GetGuid(3),
            CreatedAt = rows.GetFieldValue<DateTimeOffset>(4)
        } : null;
    }
    public Task<Result<MyServerPage>> ListAsync(AccountActor actor, int limit, string? cursor, CancellationToken ct) => ExecuteAsync(async () =>
    {
        if (limit is < 1 or > 50)
            throw new RequestFailure(new ValidationError("VALIDATION_FAILED", "Invalid page size.", new Dictionary<string, string[]> { { "limit", ["Limit must be between 1 and 50."] } }));
        Guid? last = cursor is null ? null : cursors.Decode(cursor, actor.UserId, limit);
        await using var scope = await scopes.OpenAsync(ct);
        var lease = await CheckAsync(actor, scope, ct);
        var items = await reader.ListAsync(scope, actor.UserId, limit, last, ct);
        string? next = null;
        if (items.Count > limit)
        {
            items.RemoveAt(items.Count - 1);
            next = cursors.Encode(actor.UserId, limit, items[^1].Id);
        }
        EnsureLease(lease);
        await scope.CommitAsync(ct);
        return new MyServerPage(items, next);
    });
    public Task<Result<ServerSummary>> GetAsync(AccountActor actor, Guid id, CancellationToken ct) => ReadAsync(actor,
        scope => reader.GetAsync(scope, actor.UserId, id, ct), ct);
    public Task<Result<MembershipView>> GetMembershipAsync(AccountActor actor, Guid id, CancellationToken ct) => ReadAsync(actor,
        scope => reader.MembershipAsync(scope, actor.UserId, id, ct), ct);
    public Task<Result<ServerPage>> SearchAsync(AccountActor actor, string? query, int limit, string? cursor, CancellationToken ct) => ExecuteAsync(async () =>
    {
        var errors = new Dictionary<string, string[]>();
        var text = query is null ? "" : UnicodeTextPolicy.TrimWhitespace(query);
        if (!UnicodeTextPolicy.IsValidName(text, 2, 100)) errors["q"] = ["Query must contain 2–100 UTF-16 units of single-line text."];
        if (limit is < 1 or > 50) errors["limit"] = ["Limit must be between 1 and 50."];
        if (errors.Count > 0) throw new RequestFailure(new ValidationError("VALIDATION_FAILED", "Invalid search query.", errors));
        var key = UnicodeTextPolicy.NormalizeNameKey(text);
        var position = cursor is null ? null : searchCursors.Decode(cursor, actor.UserId, key, limit);
        await using var scope = await scopes.OpenAsync(ct);
        var lease = await CheckAsync(actor, scope, ct);
        var matches = await reader.SearchAsync(scope, key, limit, position, ct);
        string? next = null;
        if (matches.Count > limit)
        {
            matches.RemoveAt(matches.Count - 1);
            next = searchCursors.Encode(actor.UserId, key, limit, matches[^1].Position);
        }
        EnsureLease(lease);
        await scope.CommitAsync(ct);
        return new ServerPage(matches.Select(row => row.Server).ToArray(), next);
    });
    private Task<Result<T>> ReadAsync<T>(AccountActor actor, Func<RelationalWorkScope, Task<T?>> read, CancellationToken ct) where T : class => ExecuteAsync(async () =>
    {
        await using var scope = await scopes.OpenAsync(ct);
        var lease = await CheckAsync(actor, scope, ct);
        var value = await read(scope) ?? throw NotFound();
        EnsureLease(lease);
        await scope.CommitAsync(ct);
        return value;
    });
    private async Task<AccountAccessCheck> CheckAsync(AccountActor actor, RelationalWorkScope scope, CancellationToken ct)
    {
        AccountAccessCheck lease;
        try
        {
            lease = await guard.AcquireAsync(actor, scope.Transaction, ct);
        }
        catch (NpgsqlException) { throw new RequestFailure(Error.ServiceUnavailable("ACCESS_CHECK_UNAVAILABLE", "Account access could not be checked.")); }
        if (lease.Status == AccountAccessStatus.AccountDenied)
            throw new RequestFailure(Error.Forbidden("ACCOUNT_ACCESS_DENIED", "An active account with verified primary email is required."));
        EnsureLease(lease);
        return lease;
    }
    private void EnsureLease(AccountAccessCheck lease)
    {
        if (!lease.IsAllowedAt(clock.GetUtcNow()))
            throw new RequestFailure(Error.Unauthorized("SESSION_INVALID", "The session is no longer valid."));
    }
    private async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return Result.Success(await action());
        }
        catch (RequestFailure ex) { return Result.Failure<T>(ex.Error); }
        catch (FingerprintKeyUnavailableException) { return Result.Failure<T>(Error.ServiceUnavailable("FINGERPRINT_KEY_UNAVAILABLE", "The operation key is unavailable.")); }
        catch (InvalidCursorException) { return Result.Failure<T>(Error.Validation("CURSOR_INVALID", "Cursor is invalid or expired.")); }
        catch (CursorKeyUnavailableException) { return Result.Failure<T>(Error.ServiceUnavailable("CURSOR_KEY_UNAVAILABLE", "Cursor keys are unavailable.")); }
        catch (Exception ex) when (IsTemporaryDatabaseFailure(ex))
        {
            logger.LogWarning("Community database operation was interrupted ({FailureType}).", ex.GetType().Name);
            return Result.Failure<T>(Error.ServiceUnavailable("COMMUNITY_TEMPORARILY_UNAVAILABLE", "Community is temporarily unavailable. Reconcile using the same operation key."));
        }
    }
    private static bool IsTemporaryDatabaseFailure(Exception ex)
    {
        // The provider's non-retrying strategy can wrap a transient failure in InvalidOperationException.
        for (Exception? cause = ex; cause is not null; cause = cause.InnerException)
            if (cause is NpgsqlException { IsTransient: true } || cause is PostgresException { SqlState: "55P03" or "40P01" or "40001" or "57014" })
                return true;
        return false;
    }
    private static NormalizedCreate Normalize(CreateServerCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        Span<byte> idBytes = stackalloc byte[16];
        command.ClientOperationId.TryWriteBytes(idBytes, bigEndian: true, out _);
        if (command.ClientOperationId.Version != 4 || (idBytes[8] & 0xc0) != 0x80)
            errors["clientOperationId"] = ["A UUIDv4 with RFC variant is required."];
        var name = command.Name;
        if (name is null || !UnicodeTextPolicy.IsValidUnicode(name))
            errors["name"] = ["A valid Unicode name is required."];
        else
        {
            name = UnicodeTextPolicy.TrimWhitespace(name);
            if (!UnicodeTextPolicy.IsValidName(name, 2, 100))
                errors["name"] = ["Name must contain 2–100 UTF-16 units of single-line text."];
        }
        var description = UnicodeTextPolicy.NormalizeDescription(command.Description);
        if (description is not null && (!UnicodeTextPolicy.IsValidUnicode(description) || description.Length > 1000))
            errors["description"] = ["Description must be valid Unicode with at most 1,000 UTF-16 units."];
        if (command.Visibility is not ("public" or "private"))
            errors["visibility"] = ["Visibility must be public or private."];
        if (errors.Count > 0)
            throw new RequestFailure(new ValidationError("VALIDATION_FAILED", "Invalid server data.", errors));
        return new(command.ClientOperationId, name!, description, command.Visibility == "private" ? (short)2 : (short)1);
    }
}
