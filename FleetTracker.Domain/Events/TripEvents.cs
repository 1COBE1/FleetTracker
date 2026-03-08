namespace FleetTracker.Domain.Events;

public record TripStarted(
    Guid TripId,
    Guid VehicleId,
    string VehicleName,
    string DriverName,
    List<PlannedStop> PlannedStops,
    DateTimeOffset StartedAt
);

public record LocationUpdated(
    Guid TripId,
    Guid VehicleId,
    double Latitude,
    double Longitude,
    double Speed,
    double Heading,
    DateTimeOffset Timestamp
);

public record EtaChanged(
    Guid TripId,
    Guid StopId,
    DateTime PreviousEta,
    DateTime NewEta
);

public record HarshBrakeReported(
    Guid TripId,
    Guid VehicleId,
    double Latitude,
    double Longitude,
    double DecelerationG,
    DateTimeOffset Timestamp
);

public record TripEnded(
    Guid TripId,
    Guid VehicleId,
    double TotalDistance,
    int StopsCompleted,
    DateTimeOffset EndedAt
);

public record PlannedStop(
    Guid StopId,
    int SequenceNumber,
    string Name,
    string Address,
    double Latitude,
    double Longitude
);

// NEW
public record StopCompleted(
    Guid TripId,
    Guid StopId,
    int SequenceNumber,
    string StopName,
    DateTimeOffset ArrivedAt
);
