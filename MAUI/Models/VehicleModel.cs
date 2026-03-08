namespace MAUI.Models;

public class VehicleModel
{
    public Guid VehicleId { get; set; }
    public string VehicleName { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? Speed { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? ActiveTripId { get; set; }
    public DateTime LastUpdateUtc { get; set; }
}
