using System.Data.Common;
using Npgsql;
using SCDC.Contracts.Messaging;

namespace SCDC.Modules.Messaging.Infrastructure;

internal sealed class ChatSpaceLifecycle : IChatSpaceLifecycle
{
    public async Task CreateChannelAsync(Guid channelId, Guid actorId, DateTimeOffset createdAt,
        DbTransaction transaction, CancellationToken cancellationToken)
    {
        if (transaction is not NpgsqlTransaction pg || pg.Connection is null)
            throw new InvalidOperationException("A live shared PostgreSQL transaction is required.");
        await using var command = new NpgsqlCommand("""
            INSERT INTO messaging.spaces(id,space_type,created_by_user_id,created_at,updated_at)
            VALUES(@id,3,@actor,@now,@now)
            """, pg.Connection, pg) { CommandTimeout = 5 };
        command.Parameters.AddWithValue("id", channelId);
        command.Parameters.AddWithValue("actor", actorId);
        command.Parameters.AddWithValue("now", createdAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
