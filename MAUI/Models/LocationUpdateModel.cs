namespace MAUI.Models;

public class LocationUpdateModel
{
    public Guid VehicleId { get; set; }
    public Guid TripId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Speed { get; set; }
    public double Heading { get; set; }
}
