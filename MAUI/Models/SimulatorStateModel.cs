namespace MAUI.Models;

public class SimulatorStateModel
{
    public Guid TripId { get; set; }
    public Guid VehicleId { get; set; }
    public string VehicleName { get; set; } = string.Empty;
    public List<SimulatorWaypoint> Waypoints { get; set; } = new();
    public int CurrentWaypointIndex { get; set; }
    public bool IsRunning { get; set; }
}

public class SimulatorWaypoint
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Speed { get; set; } // NEW
}

