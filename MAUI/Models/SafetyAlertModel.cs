namespace MAUI.Models;

public class SafetyAlertModel
{
    public Guid AlertId { get; set; }
    public Guid TripId { get; set; }
    public Guid VehicleId { get; set; }
    public string VehicleName { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DecelerationG { get; set; }
    public DateTime OccurredAt { get; set; }
    public bool IsAcknowledged { get; set; }
}
