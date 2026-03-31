using CommunityToolkit.Mvvm.ComponentModel;

namespace MAUI.Models;

public class TripReplayModel
{
    public Guid TripId { get; set; }
    public string VehicleName { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public List<TripReplayStopModel> PlannedStops { get; set; } = new();
    public List<TripReplayEventModel> Events { get; set; } = new();
}

public class TripReplayStopModel
{
    public Guid StopId { get; set; }
    public int SequenceNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class TripReplayEventModel
{
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? Speed { get; set; }
    public double? Heading { get; set; }
    public Guid? StopId { get; set; }
    public string? StopName { get; set; }
    public int? SequenceNumber { get; set; }
    public double? DecelerationG { get; set; }
}

// Represents one row in the event log panel
public partial class ReplayEventLogItem : ObservableObject
{
    // Highlights when this event is the one currently active during playback
    [ObservableProperty]
    private bool isCurrent;

    public string Time { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;

    // Index into the full _events list so the VM can look it up during seek
    public int EventIndex { get; init; }
}
