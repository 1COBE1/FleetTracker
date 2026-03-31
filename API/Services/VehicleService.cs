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

        // Persist → ReadModelHandler: InsertVehicleAsync
        await _eventStore.AppendAsync($"vehicle-{vehicleId}",
            new VehicleRegistered(vehicleId, licensePlate, model));

        return vehicleId;
    }

    public async Task<IEnumerable<dynamic>> GetAllVehiclesAsync()
        => await _vehicleRepository.GetAllVehiclesAsync();
}
