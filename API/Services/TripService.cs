using System.Text.Json;
using FleetTracker.Application.DTOs;
using FleetTracker.Domain.Events;
using FleetTracker.Infrastructure.EventStore;
using FleetTracker.Infrastructure.Repositories;

namespace FleetTracker.API.Services;

public class TripService
{
    private readonly SqlEventStore _eventStore;
    private readonly TripRepository _tripRepository;
    private readonly LocationRepository _locationRepository;

    public TripService(
        SqlEventStore eventStore,
        TripRepository tripRepository,
        LocationRepository locationRepository)
    {
        _eventStore = eventStore;
        _tripRepository = tripRepository;
        _locationRepository = locationRepository;
    }

    public async Task<Guid> StartTripAsync(
        Guid vehicleId, string vehicleName, string driverName, List<PlannedStop> stops)
    {
        var tripId = Guid.NewGuid();

        // Persist → ReadModelHandler: InsertTripAsync + InsertStopsAsync + UpdateVehicleStatus
        //         → SignalRHandler: reads fresh row, pushes TripStatusChanged
        await _eventStore.AppendAsync($"trip-{tripId}",
            new TripStarted(tripId, vehicleId, vehicleName, driverName, stops, DateTimeOffset.UtcNow));

        return tripId;
    }

    public async Task EndTripAsync(Guid tripId)
    {
        var trip = await _tripRepository.GetTripByIdAsync(tripId)
            ?? throw new Exception($"Trip {tripId} not found");

        // Persist → ReadModelHandler: EndTripAsync + UpdateVehicleStatus(Idle)
        //         → SignalRHandler: reads updated row, pushes TripStatusChanged
        await _eventStore.AppendAsync($"trip-{tripId}",
            new TripEnded(
                tripId,
                (Guid)trip.VehicleId,
                ToDouble(trip.DistanceTraveled),
                ToInt(trip.StopsCompleted),
                DateTimeOffset.UtcNow));
    }

    // Read-side — unchanged
    public async Task<dynamic?> GetTripByIdAsync(Guid tripId)
        => await _tripRepository.GetTripByIdAsync(tripId);

    public async Task<IEnumerable<dynamic>> GetActiveTripsAsync()
        => await _tripRepository.GetActiveTripsAsync();

    public async Task<IEnumerable<dynamic>> GetCompletedTripsAsync()
        => await _tripRepository.GetCompletedTripsAsync();

    public async Task<IEnumerable<dynamic>> GetStopsByTripIdAsync(Guid tripId)
        => await _tripRepository.GetStopsByTripIdAsync(tripId);

    public async Task<IEnumerable<dynamic>> GetAlertsByTripIdAsync(Guid tripId)
        => await _locationRepository.GetAlertsByTripAsync(tripId);

    public async Task<TripReplayDto?> GetTripReplayAsync(Guid tripId)
    {
        var rows = await _eventStore.ReadStreamAsync($"trip-{tripId}");
        if (rows.Count == 0) return null;

        string vehicleName = "", driverName = "";
        DateTimeOffset startedAt = default, endedAt = default;
        bool hasEnded = false;
        var plannedStops = new List<TripStopDto>();
        var events = new List<TripReplayEventDto>();

        foreach (var (eventType, eventData) in rows)
        {
            switch (eventType)
            {
                case nameof(TripStarted):
                {
                    var e = JsonSerializer.Deserialize<TripStarted>(eventData)!;
                    vehicleName = e.VehicleName;
                    driverName = e.DriverName;
                    startedAt = e.StartedAt;
                    plannedStops = e.PlannedStops
                        .Select(s => new TripStopDto(s.StopId, s.SequenceNumber, s.Name, s.Address, s.Latitude, s.Longitude))
                        .ToList();
                    events.Add(new TripReplayEventDto("TripStarted", e.StartedAt, null, null, null, null, null, null, null, null));
                    break;
                }
                case nameof(LocationUpdated):
                {
                    var e = JsonSerializer.Deserialize<LocationUpdated>(eventData)!;
                    events.Add(new TripReplayEventDto("LocationUpdated", e.Timestamp, e.Latitude, e.Longitude, e.Speed, e.Heading, null, null, null, null));
                    break;
                }
                case nameof(StopCompleted):
                {
                    var e = JsonSerializer.Deserialize<StopCompleted>(eventData)!;
                    events.Add(new TripReplayEventDto("StopCompleted", e.ArrivedAt, null, null, null, null, e.StopId, e.StopName, e.SequenceNumber, null));
                    break;
                }
                case nameof(HarshBrakeReported):
                {
                    var e = JsonSerializer.Deserialize<HarshBrakeReported>(eventData)!;
                    events.Add(new TripReplayEventDto("HarshBrakeReported", e.Timestamp, e.Latitude, e.Longitude, null, null, null, null, null, e.DecelerationG));
                    break;
                }
                case nameof(TripEnded):
                {
                    var e = JsonSerializer.Deserialize<TripEnded>(eventData)!;
                    endedAt = e.EndedAt;
                    hasEnded = true;
                    events.Add(new TripReplayEventDto("TripEnded", e.EndedAt, null, null, null, null, null, null, null, null));
                    break;
                }
            }
        }

        return new TripReplayDto(tripId, vehicleName, driverName, startedAt, hasEnded ? endedAt : null, plannedStops, events);
    }

    private static double ToDouble(object v) => v == null || v is DBNull ? 0.0 : Convert.ToDouble(v);
    private static int ToInt(object v) => v == null || v is DBNull ? 0 : Convert.ToInt32(v);
}
