using Dapper;
using Microsoft.Data.SqlClient;
using FleetTracker.Domain.Events;

namespace FleetTracker.Infrastructure.Repositories;

public class TripRepository
{
    private readonly string _connectionString;

    public TripRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task InsertTripAsync(Guid tripId, Guid vehicleId, string vehicleName, string driverName, int stopsTotal)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(@"
            INSERT INTO TripStatusView
                (TripId, VehicleId, VehicleName, DriverName, Status, StartedAt,
                 StopsTotal, StopsCompleted, AlertsCount, DistanceTraveled, CurrentSpeed)
            VALUES
                (@TripId, @VehicleId, @VehicleName, @DriverName, 'InProgress', @StartedAt,
                 @StopsTotal, 0, 0, 0, 0)",
            new
            {
                TripId = tripId,
                VehicleId = vehicleId,
                VehicleName = vehicleName,
                DriverName = driverName,
                StartedAt = DateTime.UtcNow,
                StopsTotal = stopsTotal
            });
    }

    public async Task InsertStopsAsync(Guid tripId, List<PlannedStop> stops)
    {
        using var connection = new SqlConnection(_connectionString);
        foreach (var stop in stops)
        {
            await connection.ExecuteAsync(@"
                INSERT INTO TripStopsView
                    (StopId, TripId, SequenceNumber, Name, Address,
                     Latitude, Longitude, Status)
                VALUES
                    (@StopId, @TripId, @SequenceNumber, @Name, @Address,
                     @Latitude, @Longitude, 'Pending')",
                new
                {
                    StopId = stop.StopId,
                    TripId = tripId,
                    SequenceNumber = stop.SequenceNumber,
                    Name = stop.Name,
                    Address = stop.Address,
                    Latitude = stop.Latitude,
                    Longitude = stop.Longitude
                });
        }
    }

    public async Task UpdateTripLocationAsync(Guid tripId, double speed, double distanceDelta)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(@"
            UPDATE TripStatusView
            SET CurrentSpeed     = @Speed,
                DistanceTraveled = DistanceTraveled + @DistanceDelta
            WHERE TripId = @TripId",
            new { TripId = tripId, Speed = speed, DistanceDelta = distanceDelta });
    }

    public async Task UpdateTripEtaAsync(Guid tripId, DateTime eta)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(@"
            UPDATE TripStatusView
            SET EstimatedArrival = @Eta
            WHERE TripId = @TripId",
            new { TripId = tripId, Eta = eta });
    }

    public async Task EndTripAsync(Guid tripId)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(@"
            UPDATE TripStatusView
            SET Status  = 'Completed',
                EndedAt = @EndedAt
            WHERE TripId = @TripId",
            new { TripId = tripId, EndedAt = DateTime.UtcNow });
    }

    public async Task IncrementAlertsCountAsync(Guid tripId)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(@"
            UPDATE TripStatusView
            SET AlertsCount = AlertsCount + 1
            WHERE TripId = @TripId",
            new { TripId = tripId });
    }

    public async Task UpdateStopStatusAsync(Guid stopId, string status, DateTime? actualArrival = null)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(@"
            UPDATE TripStopsView
            SET Status        = @Status,
                ActualArrival = @ActualArrival
            WHERE StopId = @StopId",
            new { StopId = stopId, Status = status, ActualArrival = actualArrival });
    }

    public async Task IncrementStopsCompletedAsync(Guid tripId)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(@"
            UPDATE TripStatusView
            SET StopsCompleted = StopsCompleted + 1
            WHERE TripId = @TripId",
            new { TripId = tripId });
    }

    public async Task UpdateStopEtaAsync(Guid stopId, DateTime eta)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(@"
            UPDATE TripStopsView
            SET EstimatedArrival = @Eta
            WHERE StopId = @StopId",
            new { StopId = stopId, Eta = eta });
    }

    public async Task<dynamic?> GetTripByIdAsync(Guid tripId)
    {
        using var connection = new SqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync(@"
            SELECT TripId, VehicleId, VehicleName, DriverName,
                   Status, StartedAt, EndedAt, EstimatedArrival,
                   DistanceTraveled, CurrentSpeed,
                   StopsTotal, StopsCompleted, AlertsCount
            FROM TripStatusView
            WHERE TripId = @TripId",
            new { TripId = tripId });
    }

    public async Task<IEnumerable<dynamic>> GetActiveTripsAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryAsync(@"
            SELECT TripId, VehicleId, VehicleName, DriverName,
                   Status, StartedAt, EstimatedArrival,
                   DistanceTraveled, CurrentSpeed,
                   StopsTotal, StopsCompleted, AlertsCount
            FROM TripStatusView
            WHERE Status = 'InProgress'
            ORDER BY StartedAt DESC");
    }

    public async Task<IEnumerable<dynamic>> GetStopsByTripIdAsync(Guid tripId)
    {
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryAsync(@"
            SELECT StopId, TripId, SequenceNumber,
                   Name, Address, Latitude, Longitude,
                   Status, EstimatedArrival, ActualArrival
            FROM TripStopsView
            WHERE TripId = @TripId
            ORDER BY SequenceNumber",
            new { TripId = tripId });
    }
}
