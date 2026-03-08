namespace MAUI.Models;

public class StopCompletedModel
{
    public Guid TripId { get; set; }
    public Guid StopId { get; set; }
    public string StopName { get; set; } = string.Empty;
    public DateTime ArrivedAt { get; set; }
}
