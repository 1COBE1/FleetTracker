namespace FleetTracker.Application.DTOs;

public record FleetUpdateDto(
    Guid VehicleId,
    double Latitude,
    double Longitude,
    double Speed,
    Guid TripId
);

public record StopCompletedDto(
    Guid TripId,
    Guid StopId,
    string StopName,
    DateTimeOffset ArrivedAt
);

public record SafetyAlertDto(
    Guid VehicleId,
    string VehicleName,
    double Latitude,
    double Longitude,
    double DecelerationG,
    DateTimeOffset OccurredAt
);
