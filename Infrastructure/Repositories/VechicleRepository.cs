using Dapper;
using Microsoft.Data.SqlClient;

namespace FleetTracker.Infrastructure.Repositories;

public class VehicleRepository
{
    private readonly string _connectionString;

    public VehicleRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task InsertVehicleAsync(Guid vehicleId, string licensePlate)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(@"
        INSERT INTO LiveMapView
            (VehicleId, VehicleName, LicensePlate, Latitude, Longitude, Speed, Status, LastUpdateUtc)
        VALUES
            (@VehicleId, @LicensePlate, @LicensePlate, 0, 0, 0, 'Idle', @Now)",
            new
            {
                VehicleId = vehicleId,
                LicensePlate = licensePlate,
                Now = DateTime.UtcNow
            });
    }


    public async Task<IEnumerable<dynamic>> GetAllVehiclesAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryAsync(@"
            SELECT VehicleId, VehicleName, LicensePlate,
                   Latitude, Longitude, Speed,
                   Status, ActiveTripId, LastUpdateUtc
            FROM LiveMapView");
    }

    public async Task UpdateVehicleStatusAsync(Guid vehicleId, string status, Guid? activeTripId)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(@"
            UPDATE LiveMapView
            SET Status = @Status, ActiveTripId = @ActiveTripId
            WHERE VehicleId = @VehicleId",
            new { VehicleId = vehicleId, Status = status, ActiveTripId = activeTripId });
    }

    public async Task UpdateVehicleLocationAsync(Guid vehicleId, double latitude, double longitude, double speed)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(@"
            UPDATE LiveMapView
            SET Latitude = @Latitude, Longitude = @Longitude,
                Speed = @Speed, Status = 'Active', LastUpdateUtc = @Now
            WHERE VehicleId = @VehicleId",
            new
            {
                VehicleId = vehicleId,
                Latitude = latitude,
                Longitude = longitude,
                Speed = speed,
                Now = DateTime.UtcNow
            });
    }
}
