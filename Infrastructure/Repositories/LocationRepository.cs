using Dapper;
using Microsoft.Data.SqlClient;

namespace FleetTracker.Infrastructure.Repositories;

public class LocationRepository
{
    private readonly string _connectionString;

    public LocationRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task InsertAlertAsync(Guid tripId, Guid vehicleId, string vehicleName, double latitude, double longitude, double decelerationG)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(@"
            INSERT INTO SafetyAlertsView
                (TripId, VehicleId, VehicleName, AlertType, Latitude, Longitude, DecelerationG, OccurredAt)
            VALUES
                (@TripId, @VehicleId, @VehicleName, 'HarshBrake', @Latitude, @Longitude, @DecelerationG, @OccurredAt)",
            new
            {
                TripId = tripId,
                VehicleId = vehicleId,
                VehicleName = vehicleName,
                Latitude = latitude,
                Longitude = longitude,
                DecelerationG = decelerationG,
                OccurredAt = DateTime.UtcNow
            });
    }

    public async Task<IEnumerable<dynamic>> GetAllVehiclePositionsAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryAsync(@"
            SELECT VehicleId, VehicleName, LicensePlate,
                   Latitude, Longitude, Speed,
                   Status, ActiveTripId, LastUpdateUtc
            FROM LiveMapView
            ORDER BY VehicleName");
    }

    public async Task<IEnumerable<dynamic>> GetSafetyAlertsAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryAsync(@"
            SELECT AlertId, TripId, VehicleId, VehicleName,
                   AlertType, Latitude, Longitude,
                   DecelerationG, OccurredAt, IsAcknowledged
            FROM SafetyAlertsView
            ORDER BY OccurredAt DESC");
    }

    public async Task<IEnumerable<dynamic>> GetAlertsByTripAsync(Guid tripId)
    {
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryAsync(@"
            SELECT AlertId, VehicleId, VehicleName,
                   AlertType, Latitude, Longitude,
                   DecelerationG, OccurredAt
            FROM SafetyAlertsView
            WHERE TripId = @TripId
            ORDER BY OccurredAt DESC",
            new { TripId = tripId });
    }
}
