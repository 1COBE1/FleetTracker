using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MAUI.Models;
using MAUI.Services;
using System.Collections.ObjectModel;

namespace MAUI.ViewModels;

public partial class TripReplayViewModel : ObservableObject
{
    private readonly TripService _tripService;

    private List<TripReplayEventModel> _events = new();

    // Log entries for significant events only (no LocationUpdated spam)
    private List<ReplayEventLogItem> _significantLog = new();
    private ReplayEventLogItem? _currentLogItem;

    private int _currentIndex = -1;
    private CancellationTokenSource? _cts;
    private double _speedMultiplier = 1.0;
    private TimeSpan _totalDuration = TimeSpan.Zero;

    [ObservableProperty]
    private TripReplayModel? replayData;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isPlaying;

    [ObservableProperty]
    private double progress;

    // Wall-clock time of the current event (e.g. "14:32:15")
    [ObservableProperty]
    private string currentTime = "--:--:--";

    // Total trip duration (e.g. "26:27")
    [ObservableProperty]
    private string totalTime = "0:00";

    [ObservableProperty]
    private string selectedSpeedLabel = "1×";

    [ObservableProperty]
    private string playPauseIcon = "▶";

    // One-liner summary of the current event shown below the map
    [ObservableProperty]
    private string currentEventDescription = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ReplayEventLogItem> eventLog = new();

    public List<string> SpeedLabels { get; } = new() { "1×", "2×", "5×", "10×", "20×" };

    // Events consumed by the page to drive the map
    public event Action<double, double>? OnVehicleUpdated;
    public event Action<Guid, string>? OnStopStatusChanged;
    public event Action<IEnumerable<TripReplayStopModel>>? OnStopsLoaded;
    public event Action<List<(double Lat, double Lng)>>? OnSeekRoute;

    // Fired when the active log item changes — page scrolls the CollectionView
    public event Action<ReplayEventLogItem>? OnLogScrollRequested;

    public TripReplayViewModel(TripService tripService)
    {
        _tripService = tripService;
    }

    public async Task LoadAsync(Guid tripId)
    {
        IsLoading = true;
        try
        {
            ReplayData = await _tripService.GetTripReplayAsync(tripId);
            if (ReplayData is null) return;

            _events = ReplayData.Events;
            _currentIndex = -1;
            IsPlaying = false;
            PlayPauseIcon = "▶";
            Progress = 0;
            CurrentEventDescription = string.Empty;
            _currentLogItem = null;

            if (_events.Count > 1)
            {
                _totalDuration = _events.Last().Timestamp - _events.First().Timestamp;
                TotalTime = FormatDuration(_totalDuration);
                CurrentTime = _events[0].Timestamp.ToLocalTime().ToString("HH:mm:ss");
            }
            else
            {
                _totalDuration = TimeSpan.Zero;
                TotalTime = "0:00";
                CurrentTime = "--:--:--";
            }

            // Build the event log from significant events only
            _significantLog = _events
                .Select((e, i) => new { e, i })
                .Where(x => x.e.EventType is not "LocationUpdated")
                .Select(x => new ReplayEventLogItem
                {
                    Time = x.e.Timestamp.ToLocalTime().ToString("HH:mm:ss"),
                    Description = FormatEventDescription(x.e),
                    EventType = x.e.EventType,
                    EventIndex = x.i
                })
                .ToList();

            EventLog = new ObservableCollection<ReplayEventLogItem>(_significantLog);

            if (ReplayData.PlannedStops.Count > 0)
                OnStopsLoaded?.Invoke(ReplayData.PlannedStops);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // AllowConcurrentExecutions = true is required so the command stays
    // invokable while PlayAsync is awaiting in the background, letting the
    // user press pause while playback is running.
    [RelayCommand(AllowConcurrentExecutions = true)]
    public async Task PlayPauseAsync()
    {
        if (IsPlaying)
            Pause();
        else
            await PlayAsync();
    }

    [RelayCommand]
    public async Task RewindAsync()
    {
        _cts?.Cancel();
        IsPlaying = false;
        PlayPauseIcon = "▶";
        _currentIndex = -1;
        Progress = 0;
        CurrentEventDescription = string.Empty;

        if (_events.Count > 0)
            CurrentTime = _events[0].Timestamp.ToLocalTime().ToString("HH:mm:ss");

        ClearLogHighlight();

        OnSeekRoute?.Invoke(new List<(double, double)>());
        if (ReplayData is not null)
            OnStopsLoaded?.Invoke(ReplayData.PlannedStops);
    }

    public void Pause()
    {
        _cts?.Cancel();
        IsPlaying = false;
        PlayPauseIcon = "▶";
    }

    public async Task PlayAsync()
    {
        if (_events.Count == 0) return;

        // Guard against double-start when AllowConcurrentExecutions = true
        if (IsPlaying) return;

        IsPlaying = true;
        PlayPauseIcon = "⏸";

        // Restart from beginning if playback has already reached the end
        if (_currentIndex >= _events.Count - 1)
        {
            _currentIndex = -1;
            Progress = 0;
            CurrentTime = _events[0].Timestamp.ToLocalTime().ToString("HH:mm:ss");
            ClearLogHighlight();
            OnSeekRoute?.Invoke(new List<(double, double)>());
            if (ReplayData is not null)
                OnStopsLoaded?.Invoke(ReplayData.PlannedStops);
            await Task.Delay(150);
        }

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            while (_currentIndex < _events.Count - 1 && !token.IsCancellationRequested)
            {
                _currentIndex++;
                var currentEvent = _events[_currentIndex];

                // Check before touching the map — a seek may have just cancelled us
                if (token.IsCancellationRequested) break;

                ApplyEventToMap(currentEvent, _currentIndex, token);

                var elapsed = currentEvent.Timestamp - _events[0].Timestamp;

                // Guard inside the lambda: this closure is queued on the main thread and
                // may execute AFTER a seek has already written correct values, so skip it
                // if the token has been cancelled.
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (token.IsCancellationRequested) return;
                    Progress = _totalDuration.TotalMilliseconds > 0
                        ? Math.Clamp(elapsed.TotalMilliseconds / _totalDuration.TotalMilliseconds, 0, 1)
                        : 0;
                    CurrentTime = currentEvent.Timestamp.ToLocalTime().ToString("HH:mm:ss");
                    CurrentEventDescription = FormatCurrentEventDescription(currentEvent);
                });

                if (_currentIndex < _events.Count - 1 && !token.IsCancellationRequested)
                {
                    var next = _events[_currentIndex + 1];
                    var realDelayMs = (next.Timestamp - currentEvent.Timestamp).TotalMilliseconds;
                    var delayMs = (int)Math.Clamp(realDelayMs / _speedMultiplier, 50, 3000);
                    await Task.Delay(delayMs, token);
                }
            }

            if (!token.IsCancellationRequested)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsPlaying = false;
                    PlayPauseIcon = "▶";
                    Progress = 1.0;
                });
            }
        }
        catch (OperationCanceledException)
        {
            // Normal pause or seek — no-op
        }
    }

    public async Task SeekToAsync(double targetProgress)
    {
        if (_events.Count == 0) return;

        _cts?.Cancel();

        var targetIndex = (int)Math.Round(targetProgress * (_events.Count - 1));
        targetIndex = Math.Clamp(targetIndex, 0, _events.Count - 1);
        _currentIndex = targetIndex;

        var targetEvent = _events[targetIndex];

        // Collect all route points up to this frame
        var routePoints = _events
            .Take(targetIndex + 1)
            .Where(e => e.EventType == "LocationUpdated" && e.Latitude.HasValue && e.Longitude.HasValue)
            .Select(e => (e.Latitude!.Value, e.Longitude!.Value))
            .ToList();

        OnSeekRoute?.Invoke(routePoints);

        // Re-add all stops as Pending then mark completed ones
        if (ReplayData is not null)
            OnStopsLoaded?.Invoke(ReplayData.PlannedStops);

        foreach (var ev in _events.Take(targetIndex + 1))
        {
            if (ev.EventType == "StopCompleted" && ev.StopId.HasValue)
                OnStopStatusChanged?.Invoke(ev.StopId.Value, "Completed");
        }

        var elapsed = targetEvent.Timestamp - _events[0].Timestamp;
        Progress = targetProgress;
        CurrentTime = targetEvent.Timestamp.ToLocalTime().ToString("HH:mm:ss");
        CurrentEventDescription = FormatCurrentEventDescription(targetEvent);

        // Highlight the last significant event up to the seek point
        var lastSignificant = _significantLog
            .Where(x => x.EventIndex <= targetIndex)
            .LastOrDefault();

        if (lastSignificant is not null)
            ActivateLogItem(lastSignificant);
        else
            ClearLogHighlight();
    }

    partial void OnSelectedSpeedLabelChanged(string value)
    {
        _speedMultiplier = value switch
        {
            "2×" => 2.0,
            "5×" => 5.0,
            "10×" => 10.0,
            "20×" => 20.0,
            _ => 1.0
        };
    }

    private void ApplyEventToMap(TripReplayEventModel ev, int eventIndex, CancellationToken token)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Skip stale map updates that were queued before a seek cancelled this loop
            if (token.IsCancellationRequested) return;

            switch (ev.EventType)
            {
                case "LocationUpdated" when ev.Latitude.HasValue && ev.Longitude.HasValue:
                    OnVehicleUpdated?.Invoke(ev.Latitude.Value, ev.Longitude.Value);
                    break;
                case "StopCompleted" when ev.StopId.HasValue:
                    OnStopStatusChanged?.Invoke(ev.StopId.Value, "Completed");
                    break;
            }

            // Highlight the matching log row for significant events
            if (ev.EventType is not "LocationUpdated")
            {
                var logItem = _significantLog.FirstOrDefault(x => x.EventIndex == eventIndex);
                if (logItem is not null)
                    ActivateLogItem(logItem);
            }
        });
    }

    private void ActivateLogItem(ReplayEventLogItem item)
    {
        if (_currentLogItem is not null && _currentLogItem != item)
            _currentLogItem.IsCurrent = false;

        _currentLogItem = item;
        item.IsCurrent = true;

        OnLogScrollRequested?.Invoke(item);
    }

    private void ClearLogHighlight()
    {
        if (_currentLogItem is not null)
        {
            _currentLogItem.IsCurrent = false;
            _currentLogItem = null;
        }
    }

    // One-liner shown below the map header during playback
    private static string FormatCurrentEventDescription(TripReplayEventModel ev) =>
        ev.EventType switch
        {
            "LocationUpdated" => ev.Speed.HasValue
                ? $"{ev.Speed.Value:F0} km/h  ·  {HeadingLabel(ev.Heading)}"
                : "Moving",
            "StopCompleted" => $"Stop {ev.SequenceNumber}: {ev.StopName}  completed",
            "HarshBrakeReported" => $"Harsh brake  {ev.DecelerationG:F1}g",
            "TripStarted" => "Trip started",
            "TripEnded" => "Trip ended",
            _ => ev.EventType
        };

    // Description stored in the event log
    private static string FormatEventDescription(TripReplayEventModel ev) =>
        ev.EventType switch
        {
            "StopCompleted" => $"Stop {ev.SequenceNumber}: {ev.StopName}",
            "HarshBrakeReported" => $"Harsh brake  {ev.DecelerationG:F1}g",
            "TripStarted" => "Trip started",
            "TripEnded" => "Trip ended",
            _ => ev.EventType
        };

    private static string HeadingLabel(double? heading) =>
        heading switch
        {
            null => "",
            < 22.5 or >= 337.5 => "N",
            < 67.5 => "NE",
            < 112.5 => "E",
            < 157.5 => "SE",
            < 202.5 => "S",
            < 247.5 => "SW",
            < 292.5 => "W",
            _ => "NW"
        };

    private static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        return $"{ts.Minutes}:{ts.Seconds:D2}";
    }
}
