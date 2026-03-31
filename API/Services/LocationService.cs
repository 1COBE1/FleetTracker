using FleetTracker.Domain.Events;
using FleetTracker.Domain.ValueObjects;
using FleetTracker.Infrastructure.EventStore;
using FleetTracker.Infrastructure.Repositories;
using FleetTracker.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FleetTracker.API.Services;

public class LocationService
{
    private readonly SqlEventStore _eventStore;
    private readonly VehicleRepository _vehicleRepository;
    private readonly TripRepository _tripRepository;
    private readonly LocationRepository _locationRepository;
    private readonly IHubContext<FleetHub> _hubContext; // only for StopApproaching

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

    public async Task UpdateLocationAsync(
        Guid tripId, Guid vehicleId,
        double latitude, double longitude,
        double speed, double heading)
    {
        // Pre-write read to calculate distance delta
        var vehicles = await _vehicleRepository.GetAllVehiclesAsync();
        var current = vehicles.FirstOrDefault(v => v.VehicleId == vehicleId);
        double distanceDelta = current != null
            ? new Location(D(current.Latitude), D(current.Longitude))
                .DistanceTo(new Location(latitude, longitude))
            : 0;

        // Persist → ReadModelHandler updates LiveMapView + TripStatusView
        //         → SignalRHandler pushes FleetUpdate to MAUI
        await _eventStore.AppendAsync($"trip-{tripId}",
            new LocationUpdated(tripId, vehicleId, latitude, longitude,
                speed, heading, distanceDelta, DateTimeOffset.UtcNow));

        await CheckStopArrivalAsync(tripId, latitude, longitude, speed);
    }

    public async Task ReportHarshBrakeAsync(
        Guid tripId, Guid vehicleId, string vehicleName,
        double latitude, double longitude, double decelerationG)
    {
        // Persist → ReadModelHandler: InsertAlertAsync + IncrementAlertsCountAsync
        //         → SignalRHandler: pushes SafetyAlert to MAUI
        await _eventStore.AppendAsync($"trip-{tripId}",
            new HarshBrakeReported(tripId, vehicleId, vehicleName,
                latitude, longitude, decelerationG, DateTimeOffset.UtcNow));
    }

    public async Task<IEnumerable<dynamic>> GetAllVehiclePositionsAsync()
        => await _locationRepository.GetAllVehiclePositionsAsync();

    public async Task<IEnumerable<dynamic>> GetSafetyAlertsAsync()
        => await _locationRepository.GetSafetyAlertsAsync();

    // -------------------------------------------------------------------------

    private async Task CheckStopArrivalAsync(
        Guid tripId, double latitude, double longitude, double speed)
    {
        const double ArrivalRadiusKm = 0.10;
        const double ApproachRadiusKm = 0.50;

        var stops = await _tripRepository.GetStopsByTripIdAsync(tripId);
        var currentLoc = new Location(latitude, longitude);

        var nextStop = stops.Cast<dynamic>()
            .Where(s => S(s.Status) == "Pending" || S(s.Status) == "Active")
            .OrderBy(s => I(s.SequenceNumber))
            .FirstOrDefault();

        if (nextStop == null) return;

        var stopLoc = new Location(D(nextStop.Latitude), D(nextStop.Longitude));
        var distance = currentLoc.DistanceTo(stopLoc);

        if (distance <= ArrivalRadiusKm)
        {
            // Persist → ReadModelHandler: UpdateStopStatus + IncrementStopsCompleted
            //         → SignalRHandler: pushes StopCompleted to MAUI
            await _eventStore.AppendAsync($"trip-{tripId}",
                new StopCompleted(
                    tripId, G(nextStop.StopId), I(nextStop.SequenceNumber),
                    S(nextStop.Name), DateTimeOffset.UtcNow));

            // Re-fetch — ReadModelHandler has already marked the stop Completed
            stops = await _tripRepository.GetStopsByTripIdAsync(tripId);
            nextStop = stops.Cast<dynamic>()
                .Where(s => S(s.Status) == "Pending")
                .OrderBy(s => I(s.SequenceNumber))
                .FirstOrDefault();
        }
        else if (distance <= ApproachRadiusKm && S(nextStop.Status) == "Pending")
        {
            // StopApproaching has no domain event — direct write + direct push
            await _tripRepository.UpdateStopStatusAsync(G(nextStop.StopId), "Active");
            await _hubContext.Clients.All.SendAsync("StopApproaching", new
            {
                TripId = tripId,
                StopId = G(nextStop.StopId),
                StopName = S(nextStop.Name)
            });
        }

        // ETA recalculation
        if (nextStop != null && speed > 0)
        {
            var nextStopLoc = new Location(D(nextStop.Latitude), D(nextStop.Longitude));
            var distToNext = currentLoc.DistanceTo(nextStopLoc);
            var newEta = DateTime.UtcNow.AddHours(distToNext / speed);
            var prevEta = nextStop.EstimatedArrival is DateTime prev
    ? new DateTimeOffset(prev, TimeSpan.Zero)
    : DateTimeOffset.UtcNow;

            await _eventStore.AppendAsync($"trip-{tripId}",
                new EtaChanged(tripId, G(nextStop.StopId), prevEta, DateTimeOffset.UtcNow.AddHours(distToNext / speed)));

        }
    }

    private static double D(object v) => v == null || v is DBNull ? 0.0 : Convert.ToDouble(v);
    private static int I(object v) => v == null || v is DBNull ? 0 : Convert.ToInt32(v);
    private static string S(object v) => v == null || v is DBNull ? "" : v.ToString()!;
    private static Guid G(object v) => v == null || v is DBNull ? Guid.Empty : (Guid)v;
}
