using System.Buffers.Binary;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

// Every write that depends on a pair's block state takes this transaction lock.
// Hash collisions only serialize unrelated pairs; they cannot bypass a block.
internal static class UserBlockPairLock
{
    public static async Task LockAsync(MessagingDbContext db, Guid first, Guid second, CancellationToken ct) =>
        await LockManyAsync(db, first, [second], ct);

    public static async Task LockManyAsync(MessagingDbContext db, Guid actor,
        IEnumerable<Guid> peers, CancellationToken ct)
    {
        var transaction = db.Database.CurrentTransaction
            ?? throw new InvalidOperationException("A database transaction is required for block pair locks.");
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        foreach (var key in peers.Where(peer => peer != actor).Select(peer => Key(actor, peer)).Distinct().Order())
        {
            await using var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(@key)",
                connection, (NpgsqlTransaction)transaction.GetDbTransaction());
            command.Parameters.AddWithValue("key", key);
            await command.ExecuteScalarAsync(ct);
        }
    }

    private static long Key(Guid first, Guid second)
    {
        if (first.CompareTo(second) > 0) (first, second) = (second, first);
        Span<byte> pair = stackalloc byte[32];
        first.TryWriteBytes(pair[..16]);
        second.TryWriteBytes(pair[16..]);
        return BinaryPrimitives.ReadInt64BigEndian(SHA256.HashData(pair));
    }
}
