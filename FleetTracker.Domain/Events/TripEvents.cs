namespace FleetTracker.Domain.Events;

public record TripStarted(
    Guid TripId,
    Guid VehicleId,
    string VehicleName,
    string DriverName,
    List<PlannedStop> PlannedStops,
    DateTimeOffset StartedAt
) : DomainEvent;

public record LocationUpdated(
    Guid TripId,
    Guid VehicleId,
    double Latitude,
    double Longitude,
    double Speed,
    double Heading,
    double DistanceDelta,       // ← NEW: computed before AppendAsync, carried in event
    DateTimeOffset Timestamp
) : DomainEvent;

public record EtaChanged(
    Guid TripId,
    Guid StopId,
    DateTimeOffset PreviousEta,
    DateTimeOffset NewEta
) : DomainEvent;


public record HarshBrakeReported(
    Guid TripId,
    Guid VehicleId,
    string VehicleName,         // ← NEW: needed for replay correctness
    double Latitude,
    double Longitude,
    double DecelerationG,
    DateTimeOffset Timestamp
) : DomainEvent;

public record TripEnded(
    Guid TripId,
    Guid VehicleId,
    double TotalDistance,
    int StopsCompleted,
    DateTimeOffset EndedAt
) : DomainEvent;

public record PlannedStop(
    Guid StopId,
    int SequenceNumber,
    string Name,
    string Address,
    double Latitude,
    double Longitude
);

public record StopCompleted(
    Guid TripId,
    Guid StopId,
    int SequenceNumber,
    string StopName,
    DateTimeOffset ArrivedAt
) : DomainEvent;
