namespace MAUI.Models;

public class TripStopModel
{
    public Guid StopId { get; set; }
    public Guid TripId { get; set; }
    public int SequenceNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? EstimatedArrival { get; set; }
    public DateTime? ActualArrival { get; set; }
}
