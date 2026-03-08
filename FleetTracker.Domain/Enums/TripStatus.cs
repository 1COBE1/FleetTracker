namespace FleetTracker.Domain.Enums;

public enum TripStatus
{
    InProgress,
    Completed,
    Cancelled
}

public enum VehicleStatus
{
    Idle,
    Active,
    Offline
}

public enum StopStatus
{
    Pending,
    Active,
    Completed
}

public enum AlertType
{
    HarshBrake,
    Speeding
}
