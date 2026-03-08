using FleetTracker.Domain.Events;
using FleetTracker.Infrastructure.EventStore;
using FleetTracker.Infrastructure.Repositories;
using Microsoft.AspNetCore.SignalR;
using FleetTracker.API.Hubs;

namespace FleetTracker.API.Services;

public class TripService
{
    private readonly SqlEventStore _eventStore;
    private readonly TripRepository _tripRepository;
    private readonly VehicleRepository _vehicleRepository;
    private readonly LocationRepository _locationRepository;
    private readonly IHubContext<FleetHub> _hubContext;

    public TripService(
        SqlEventStore eventStore,
        TripRepository tripRepository,
        VehicleRepository vehicleRepository,
        LocationRepository locationRepository,
        IHubContext<FleetHub> hubContext)
    {
        _eventStore = eventStore;
        _tripRepository = tripRepository;
        _vehicleRepository = vehicleRepository;
        _locationRepository = locationRepository;
        _hubContext = hubContext;
    }

    public async Task<Guid> StartTripAsync(Guid vehicleId, string vehicleName, string driverName, List<PlannedStop> stops)
    {
        var tripId = Guid.NewGuid();

        await _eventStore.AppendAsync(
            streamId: $"trip-{tripId}",
            eventType: nameof(TripStarted),
            eventData: new TripStarted(tripId, vehicleId, vehicleName, driverName, stops, DateTimeOffset.UtcNow)
        );

        await _tripRepository.InsertTripAsync(tripId, vehicleId, vehicleName, driverName, stops.Count);
        await _tripRepository.InsertStopsAsync(tripId, stops);
        await _vehicleRepository.UpdateVehicleStatusAsync(vehicleId, "Active", tripId);

        var trip = await _tripRepository.GetTripByIdAsync(tripId);
        if (trip != null)
            await _hubContext.Clients.All.SendAsync("TripStatusChanged", (object)MapTripToPayload(trip));

        return tripId;
    }

    public async Task EndTripAsync(Guid tripId)
    {
        var trip = await _tripRepository.GetTripByIdAsync(tripId);
        if (trip == null)
            throw new Exception($"Trip {tripId} not found");

        await _eventStore.AppendAsync(
            streamId: $"trip-{tripId}",
            eventType: nameof(TripEnded),
            eventData: new TripEnded(
                tripId,
                (Guid)trip.VehicleId,
                ToDouble(trip.DistanceTraveled),
                ToInt(trip.StopsCompleted),
                DateTimeOffset.UtcNow)
        );

        await _tripRepository.EndTripAsync(tripId);
        await _vehicleRepository.UpdateVehicleStatusAsync((Guid)trip.VehicleId, "Idle", null);

        var updatedTrip = await _tripRepository.GetTripByIdAsync(tripId);
        if (updatedTrip != null)
            await _hubContext.Clients.All.SendAsync("TripStatusChanged", (object)MapTripToPayload(updatedTrip));
    }

    public async Task<IEnumerable<dynamic>> GetAlertsByTripIdAsync(Guid tripId)
        => await _locationRepository.GetAlertsByTripAsync(tripId);

    public async Task<dynamic?> GetTripByIdAsync(Guid tripId)
        => await _tripRepository.GetTripByIdAsync(tripId);

    public async Task<IEnumerable<dynamic>> GetActiveTripsAsync()
        => await _tripRepository.GetActiveTripsAsync();

    public async Task<IEnumerable<dynamic>> GetStopsByTripIdAsync(Guid tripId)
        => await _tripRepository.GetStopsByTripIdAsync(tripId);

    private static object MapTripToPayload(dynamic t) => new
    {
        TripId = (Guid)t.TripId,
        VehicleId = (Guid)t.VehicleId,
        VehicleName = (string)t.VehicleName,
        DriverName = (string)t.DriverName,
        Status = (string)t.Status,
        StartedAt = (DateTime)t.StartedAt,
        EndedAt = t.EndedAt is DateTime eda ? (DateTime?)eda : null,
        EstimatedArrival = t.EstimatedArrival is DateTime eta ? (DateTime?)eta : null,
        DistanceTraveled = ToDouble(t.DistanceTraveled),
        CurrentSpeed = ToDouble(t.CurrentSpeed),
        StopsTotal = ToInt(t.StopsTotal),
        StopsCompleted = ToInt(t.StopsCompleted),
        AlertsCount = ToInt(t.AlertsCount)
    };

    private static double ToDouble(object value)
        => value == null || value is DBNull ? 0.0 : Convert.ToDouble(value);

    private static int ToInt(object value)
        => value == null || value is DBNull ? 0 : Convert.ToInt32(value);
}
