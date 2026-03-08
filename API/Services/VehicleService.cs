using FleetTracker.Domain.Events;
using FleetTracker.Infrastructure.EventStore;
using FleetTracker.Infrastructure.Repositories;

namespace FleetTracker.API.Services;

public class VehicleService
{
    private readonly SqlEventStore _eventStore;
    private readonly VehicleRepository _vehicleRepository;

    public VehicleService(SqlEventStore eventStore, VehicleRepository vehicleRepository)
    {
        _eventStore = eventStore;
        _vehicleRepository = vehicleRepository;
    }

    public async Task<Guid> RegisterVehicleAsync(string licensePlate, string model)
    {
        var vehicleId = Guid.NewGuid();

        // 1. Append event
        await _eventStore.AppendAsync(
            streamId: $"vehicle-{vehicleId}",
            eventType: nameof(VehicleRegistered),
            eventData: new VehicleRegistered(vehicleId, licensePlate, model)
        );

        // 2. Update read model
        await _vehicleRepository.InsertVehicleAsync(vehicleId, licensePlate);

        return vehicleId;
    }

    public async Task<IEnumerable<dynamic>> GetAllVehiclesAsync()
    {
        return await _vehicleRepository.GetAllVehiclesAsync();
    }
}
