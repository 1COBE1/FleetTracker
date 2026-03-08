namespace MAUI.Models;

public class TripModel
{
    public Guid TripId { get; set; }
    public Guid VehicleId { get; set; }
    public string VehicleName { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime? EstimatedArrival { get; set; }
    public double DistanceTraveled { get; set; }
    public double CurrentSpeed { get; set; }
    public int StopsTotal { get; set; }
    public int StopsCompleted { get; set; }
    public int AlertsCount { get; set; }
}
