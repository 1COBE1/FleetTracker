using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;

namespace FleetTracker.Infrastructure.EventStore;

public class SqlEventStore
{
    private readonly string _connectionString;

    public SqlEventStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task AppendAsync(string streamId, string eventType, object eventData)
    {
        using var connection = new SqlConnection(_connectionString);

        // Calculate next version for this stream
        var version = await connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM Events WHERE StreamId = @StreamId",
            new { StreamId = streamId }) + 1;

        await connection.ExecuteAsync(@"
            INSERT INTO Events (StreamId, EventType, EventData, Version)
            VALUES (@StreamId, @EventType, @EventData, @Version)",
            new
            {
                StreamId = streamId,
                EventType = eventType,
                EventData = JsonSerializer.Serialize(eventData),
                Version = version
            });
    }

    public async Task<List<(string EventType, string EventData)>> ReadStreamAsync(string streamId)
    {
        using var connection = new SqlConnection(_connectionString);
        var rows = await connection.QueryAsync(@"
            SELECT EventType, EventData
            FROM Events
            WHERE StreamId = @StreamId
            ORDER BY Version",
            new { StreamId = streamId });

        return rows.Select(r => ((string)r.EventType, (string)r.EventData)).ToList();
    }
}
