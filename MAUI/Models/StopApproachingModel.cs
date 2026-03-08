namespace MAUI.Models;

public class StopApproachingModel
{
    public Guid TripId { get; set; }
    public Guid StopId { get; set; }
    public string StopName { get; set; } = string.Empty;
}
