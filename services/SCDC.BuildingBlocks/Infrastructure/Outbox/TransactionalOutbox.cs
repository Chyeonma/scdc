using System.Text.Json;
using NpgsqlTypes;
using SCDC.BuildingBlocks.Infrastructure.Persistence;

namespace SCDC.BuildingBlocks.Infrastructure.Outbox;

public sealed class TransactionalOutbox
{
    private readonly string _table;
    public TransactionalOutbox(string owner)
    {
        if (owner is not ("identity" or "community" or "messaging"))
            throw new ArgumentOutOfRangeException(nameof(owner));
        _table = owner + ".outbox_events";
    }

    public async Task AppendAsync(RelationalWorkScope scope, string eventType, string aggregateType, Guid aggregateId,
        int aggregateVersion, object payload, CancellationToken cancellationToken)
    {
        await using var command = scope.CreateCommand($"""
            INSERT INTO {_table}(id,event_type,aggregate_type,aggregate_id,aggregate_version,payload)
            VALUES(@id,@type,@aggregateType,@aggregateId,@version,@payload)
            """);
        command.Parameters.AddWithValue("id", Guid.CreateVersion7());
        command.Parameters.AddWithValue("type", eventType);
        command.Parameters.AddWithValue("aggregateType", aggregateType);
        command.Parameters.AddWithValue("aggregateId", aggregateId);
        command.Parameters.AddWithValue("version", aggregateVersion);
        command.Parameters.AddWithValue("payload", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(payload, JsonSerializerOptions.Web));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
