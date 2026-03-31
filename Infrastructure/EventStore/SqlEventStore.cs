using System.Text.Json;
using Dapper;
using FleetTracker.Domain.Notifications;
using FleetTracker.Domain.Events;
using MediatR;
using Microsoft.Data.SqlClient;

namespace FleetTracker.Infrastructure.EventStore;

public class SqlEventStore
{
    private readonly string _connectionString;
    private readonly IPublisher _publisher;

    public SqlEventStore(string connectionString, IPublisher publisher)
    {
        _connectionString = connectionString;
        _publisher = publisher;
    }

    public async Task AppendAsync(string streamId, DomainEvent @event)
    {
        using var connection = new SqlConnection(_connectionString);

        // MAX instead of COUNT — correct even with version gaps
        var version = await connection.ExecuteScalarAsync<int>(@"
            SELECT ISNULL(MAX(Version), 0) FROM Events WHERE StreamId = @StreamId",
            new { StreamId = streamId }) + 1;

        await connection.ExecuteAsync(@"
            INSERT INTO Events (StreamId, EventType, EventData, Version)
            VALUES (@StreamId, @EventType, @EventData, @Version)",
            new
            {
                StreamId = streamId,
                EventType = @event.GetType().Name,
                // Serialize with runtime type so derived record fields are preserved
                EventData = JsonSerializer.Serialize(@event, @event.GetType()),
                Version = version
            });

        // Only fires AFTER the INSERT succeeds — this is the guarantee
        await _publisher.Publish(new EventPersisted(@event));
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
