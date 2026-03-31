namespace FleetTracker.Domain.Events;

public record VehicleRegistered(
    Guid VehicleId,
    string LicensePlate,
    string Model
) : DomainEvent;
