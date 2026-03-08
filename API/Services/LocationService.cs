using FleetTracker.Domain.Events;
using FleetTracker.Domain.ValueObjects;
using FleetTracker.Infrastructure.EventStore;
using FleetTracker.Infrastructure.Repositories;
using Microsoft.AspNetCore.SignalR;
using FleetTracker.API.Hubs;

namespace FleetTracker.API.Services;

public class LocationService
{
    private readonly SqlEventStore _eventStore;
    private readonly VehicleRepository _vehicleRepository;
    private readonly TripRepository _tripRepository;
    private readonly LocationRepository _locationRepository;
    private readonly IHubContext<FleetHub> _hubContext;

    public LocationService(
        SqlEventStore eventStore,
        VehicleRepository vehicleRepository,
        TripRepository tripRepository,
        LocationRepository locationRepository,
        IHubContext<FleetHub> hubContext)
    {
        _eventStore = eventStore;
        _vehicleRepository = vehicleRepository;
        _tripRepository = tripRepository;
        _locationRepository = locationRepository;
        _hubContext = hubContext;
    }

    public async Task UpdateLocationAsync(Guid tripId, Guid vehicleId, double latitude, double longitude, double speed, double heading)
    {
        const double ArrivalRadiusKm = 0.10; // increase to 100m for testing
        const double ApproachRadiusKm = 0.50; // increase to 500m


        // 1. Calculate distance delta
        var vehicles = await _vehicleRepository.GetAllVehiclesAsync();
        var currentVehicle = vehicles.FirstOrDefault(v => v.VehicleId == vehicleId);

        double distanceDelta = 0;
        if (currentVehicle != null)
        {
            var previousLocation = new Location(D(currentVehicle.Latitude), D(currentVehicle.Longitude));
            var newLocation = new Location(latitude, longitude);
            distanceDelta = previousLocation.DistanceTo(newLocation);
        }

        // 2. Append event
        await _eventStore.AppendAsync(
            streamId: $"trip-{tripId}",
            eventType: nameof(LocationUpdated),
            eventData: new LocationUpdated(tripId, vehicleId, latitude, longitude, speed, heading, DateTimeOffset.UtcNow)
        );

        // 3. Update read models
        await _vehicleRepository.UpdateVehicleLocationAsync(vehicleId, latitude, longitude, speed);
        await _tripRepository.UpdateTripLocationAsync(tripId, speed, distanceDelta);

        // 4. Stop arrival detection
        var stops = await _tripRepository.GetStopsByTripIdAsync(tripId);
        var currentLoc = new Location(latitude, longitude);

        var nextStop = stops
            .Cast<dynamic>()
            .Where(s => S(s.Status) == "Pending" || S(s.Status) == "Active")
            .OrderBy(s => I(s.SequenceNumber))
            .FirstOrDefault();

        if (nextStop != null)
        {
            var stopLoc = new Location(D(nextStop.Latitude), D(nextStop.Longitude));
            var distanceToStop = currentLoc.DistanceTo(stopLoc);

            if (distanceToStop <= ArrivalRadiusKm)
            {
                var arrivedAt = DateTime.UtcNow;

                await _eventStore.AppendAsync(
                    streamId: $"trip-{tripId}",
                    eventType: nameof(StopCompleted),
                    eventData: new StopCompleted(
                        tripId,
                        G(nextStop.StopId),
                        I(nextStop.SequenceNumber),
                        S(nextStop.Name),
                        DateTimeOffset.UtcNow)
                );

                await _tripRepository.UpdateStopStatusAsync(G(nextStop.StopId), "Completed", arrivedAt);
                await _tripRepository.IncrementStopsCompletedAsync(tripId);

                await _hubContext.Clients.All.SendAsync("StopCompleted", new
                {
                    TripId = tripId,
                    StopId = G(nextStop.StopId),
                    StopName = S(nextStop.Name),
                    ArrivedAt = arrivedAt
                });

                var completedStopId = G(nextStop.StopId);
                nextStop = stops
                    .Cast<dynamic>()
                    .Where(s => S(s.Status) == "Pending" && G(s.StopId) != completedStopId)
                    .OrderBy(s => I(s.SequenceNumber))
                    .FirstOrDefault();
            }
            else if (distanceToStop <= ApproachRadiusKm && S(nextStop.Status) == "Pending")
            {
                await _tripRepository.UpdateStopStatusAsync(G(nextStop.StopId), "Active");

                await _hubContext.Clients.All.SendAsync("StopApproaching", new
                {
                    TripId = tripId,
                    StopId = G(nextStop.StopId),
                    StopName = S(nextStop.Name)
                });
            }

            if (nextStop != null && speed > 0)
            {
                var nextStopLoc = new Location(D(nextStop.Latitude), D(nextStop.Longitude));
                var distanceToNext = currentLoc.DistanceTo(nextStopLoc);
                var newEta = DateTime.UtcNow.AddHours(distanceToNext / speed);
                var previousEta = nextStop.EstimatedArrival is DateTime prev ? prev : DateTime.UtcNow;

                await _tripRepository.UpdateStopEtaAsync(G(nextStop.StopId), newEta);
                await _tripRepository.UpdateTripEtaAsync(tripId, newEta);

                await _eventStore.AppendAsync(
                    streamId: $"trip-{tripId}",
                    eventType: nameof(EtaChanged),
                    eventData: new EtaChanged(tripId, G(nextStop.StopId), previousEta, newEta)
                );
            }
        }

        // 5. Push to MAUI
        await _hubContext.Clients.All.SendAsync("FleetUpdate", new
        {
            VehicleId = vehicleId,
            Latitude = latitude,
            Longitude = longitude,
            Speed = speed,
            TripId = tripId
        });
    }

    public async Task ReportHarshBrakeAsync(Guid tripId, Guid vehicleId, string vehicleName, double latitude, double longitude, double decelerationG)
    {
        await _eventStore.AppendAsync(
            streamId: $"trip-{tripId}",
            eventType: nameof(HarshBrakeReported),
            eventData: new HarshBrakeReported(tripId, vehicleId, latitude, longitude, decelerationG, DateTimeOffset.UtcNow)
        );

        await _locationRepository.InsertAlertAsync(tripId, vehicleId, vehicleName, latitude, longitude, decelerationG);
        await _tripRepository.IncrementAlertsCountAsync(tripId);

        await _hubContext.Clients.All.SendAsync("SafetyAlert", new
        {
            VehicleId = vehicleId,
            VehicleName = vehicleName,
            Latitude = latitude,
            Longitude = longitude,
            DecelerationG = decelerationG,
            OccurredAt = DateTime.UtcNow
        });
    }

    public async Task<IEnumerable<dynamic>> GetAllVehiclePositionsAsync()
        => await _locationRepository.GetAllVehiclePositionsAsync();

    public async Task<IEnumerable<dynamic>> GetSafetyAlertsAsync()
        => await _locationRepository.GetSafetyAlertsAsync();

    // DBNull-safe helpers — short names to avoid conflicts with built-in methods
    private static double D(object v) => v == null || v is DBNull ? 0.0 : Convert.ToDouble(v);
    private static int I(object v) => v == null || v is DBNull ? 0 : Convert.ToInt32(v);
    private static string S(object v) => v == null || v is DBNull ? "" : v.ToString()!;
    private static Guid G(object v) => v == null || v is DBNull ? Guid.Empty : (Guid)v;
}
