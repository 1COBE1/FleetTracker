namespace FleetTracker.Application.DTOs;

public record TripStopDto(
    Guid StopId,
    int SequenceNumber,
    string Name,
    string Address,
    double Latitude,
    double Longitude
);

public record TripReplayEventDto(
    string EventType,
    DateTimeOffset Timestamp,
    double? Latitude,
    double? Longitude,
    double? Speed,
    double? Heading,
    Guid? StopId,
    string? StopName,
    int? SequenceNumber,
    double? DecelerationG
);

public record TripReplayDto(
    Guid TripId,
    string VehicleName,
    string DriverName,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    List<TripStopDto> PlannedStops,
    List<TripReplayEventDto> Events
);
