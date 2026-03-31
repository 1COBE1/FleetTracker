using FleetTracker.Domain.Notifications;
using FleetTracker.Domain.Events;
using FleetTracker.Infrastructure.Repositories;
using MediatR;

namespace FleetTracker.Application.Handlers;

public class ReadModelHandler : INotificationHandler<EventPersisted>
{
    private readonly VehicleRepository _vehicleRepo;
    private readonly TripRepository _tripRepo;
    private readonly LocationRepository _locationRepo;

    public ReadModelHandler(
        VehicleRepository vehicleRepo,
        TripRepository tripRepo,
        LocationRepository locationRepo)
    {
        _vehicleRepo = vehicleRepo;
        _tripRepo = tripRepo;
        _locationRepo = locationRepo;
    }

    public async Task Handle(EventPersisted notification, CancellationToken ct)
    {
        switch (notification.Event)
        {
            case LocationUpdated e:
                await _vehicleRepo.UpdateVehicleLocationAsync(e.VehicleId, e.Latitude, e.Longitude, e.Speed);
                await _tripRepo.UpdateTripLocationAsync(e.TripId, e.Speed, e.DistanceDelta);
                break;

            case StopCompleted e:
                await _tripRepo.UpdateStopStatusAsync(e.StopId, "Completed", e.ArrivedAt.UtcDateTime);
                await _tripRepo.IncrementStopsCompletedAsync(e.TripId);
                break;

            case EtaChanged e:
                await _tripRepo.UpdateStopEtaAsync(e.StopId, e.NewEta.UtcDateTime);
                await _tripRepo.UpdateTripEtaAsync(e.TripId, e.NewEta.UtcDateTime);
                break;

            case TripStarted e:
                await _tripRepo.InsertTripAsync(e.TripId, e.VehicleId, e.VehicleName, e.DriverName, e.PlannedStops.Count);
                await _tripRepo.InsertStopsAsync(e.TripId, e.PlannedStops);
                await _vehicleRepo.UpdateVehicleStatusAsync(e.VehicleId, "Active", e.TripId);
                break;

            case TripEnded e:
                await _tripRepo.EndTripAsync(e.TripId);
                await _vehicleRepo.UpdateVehicleStatusAsync(e.VehicleId, "Idle", null);
                break;

            case HarshBrakeReported e:
                await _locationRepo.InsertAlertAsync(e.TripId, e.VehicleId, e.VehicleName, e.Latitude, e.Longitude, e.DecelerationG);
                await _tripRepo.IncrementAlertsCountAsync(e.TripId);
                break;

            case VehicleRegistered e:
                await _vehicleRepo.InsertVehicleAsync(e.VehicleId, e.LicensePlate);
                break;
        }
    }
}
