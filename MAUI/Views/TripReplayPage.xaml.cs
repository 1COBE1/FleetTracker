using System.ComponentModel;
using MAUI.Models;
using MAUI.ViewModels;

namespace MAUI.Views;

public partial class TripReplayPage : ContentPage, IQueryAttributable
{
    private readonly TripReplayViewModel _vm;
    private bool _mapReady;

    // Slider coordination flags
    private bool _updatingSlider;   // true while we push a programmatic value → suppresses ValueChanged
    private bool _dragActive;       // true between DragStarted and DragCompleted
    private bool _wasPlaying;       // playback state captured when user starts interacting

    public TripReplayPage(TripReplayViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        _vm.OnVehicleUpdated += OnVehicleUpdated;
        _vm.OnStopStatusChanged += OnStopStatusChanged;
        _vm.OnStopsLoaded += OnStopsLoaded;
        _vm.OnSeekRoute += OnSeekRoute;
        _vm.OnLogScrollRequested += OnLogScrollRequested;
        _vm.PropertyChanged += OnVmPropertyChanged;

        ReplayMapWebView.Navigated += (s, e) =>
        {
            _mapReady = true;
            if (_vm.ReplayData?.PlannedStops is { Count: > 0 } stops)
                OnStopsLoaded(stops);
        };
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("tripId", out var value) && Guid.TryParse(value?.ToString(), out var tripId))
            await _vm.LoadAsync(tripId);
    }

    // ──── Slider coordination ────────────────────────────────────────────

    // When the VM's Progress changes during playback, push it to the slider.
    // Skip during a drag so we don't fight the user's thumb.
    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TripReplayViewModel.Progress)) return;
        if (_dragActive) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _updatingSlider = true;
            ReplaySlider.Value = _vm.Progress;
            _updatingSlider = false;
        });
    }

    // User starts dragging the thumb — pause and remember state
    private void OnSliderDragStarted(object sender, EventArgs e)
    {
        _dragActive = true;
        _wasPlaying = _vm.IsPlaying;
        _vm.Pause();
    }

    // User releases the thumb after a drag
    private async void OnSliderDragCompleted(object sender, EventArgs e)
    {
        _dragActive = false;
        await SeekAndResumeAsync(ReplaySlider.Value);
    }

    // Fires for every value change — both programmatic and user-initiated.
    // Catches plain clicks on the track where DragStarted/DragCompleted may
    // never fire (Windows MAUI behaviour).
    private async void OnSliderValueChanged(object sender, ValueChangedEventArgs e)
    {
        // Ignore programmatic updates and changes during a drag (DragCompleted handles those)
        if (_updatingSlider || _dragActive) return;

        // This is a plain click on the slider track
        _wasPlaying = _vm.IsPlaying;
        _vm.Pause();
        await SeekAndResumeAsync(e.NewValue);
    }

    private async Task SeekAndResumeAsync(double value)
    {
        await _vm.SeekToAsync(value);

        // Push the seeked value to the slider so it stays in sync
        _updatingSlider = true;
        ReplaySlider.Value = _vm.Progress;
        _updatingSlider = false;

        if (_wasPlaying)
            await _vm.PlayAsync();
    }

    // ──── Map callbacks ──────────────────────────────────────────────────

    private void OnVehicleUpdated(double lat, double lng)
    {
        if (!_mapReady) return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var ic = System.Globalization.CultureInfo.InvariantCulture;
            ReplayMapWebView.EvaluateJavaScriptAsync(
                $"updateVehicle({lat.ToString(ic)}, {lng.ToString(ic)});");
        });
    }

    private void OnStopStatusChanged(Guid stopId, string status)
    {
        if (!_mapReady) return;
        MainThread.BeginInvokeOnMainThread(() =>
            ReplayMapWebView.EvaluateJavaScriptAsync($"updateStopStatus('{stopId}', '{status}');"));
    }

    private void OnStopsLoaded(IEnumerable<TripReplayStopModel> stops)
    {
        if (!_mapReady) return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var ic = System.Globalization.CultureInfo.InvariantCulture;
            foreach (var stop in stops)
            {
                var lat = stop.Latitude.ToString(ic);
                var lng = stop.Longitude.ToString(ic);
                var name = stop.Name.Replace("'", "\\'");
                ReplayMapWebView.EvaluateJavaScriptAsync(
                    $"addStop('{stop.StopId}', {lat}, {lng}, '{name}', 'Pending');");
            }
            ReplayMapWebView.EvaluateJavaScriptAsync("fitToStops();");
        });
    }

    private void OnSeekRoute(List<(double Lat, double Lng)> points)
    {
        if (!_mapReady) return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var ic = System.Globalization.CultureInfo.InvariantCulture;
            var pointsJson = "[" + string.Join(",",
                points.Select(p => $"[{p.Lat.ToString(ic)},{p.Lng.ToString(ic)}]")) + "]";
            ReplayMapWebView.EvaluateJavaScriptAsync($"clearAll(); seekRoute({pointsJson});");
        });
    }

    private void OnLogScrollRequested(ReplayEventLogItem item)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                EventLogView.ScrollTo(item, position: ScrollToPosition.Center, animate: false);
            }
            catch { }
        });
    }

    // ──── Cleanup ────────────────────────────────────────────────────────

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Pause();
        _vm.OnVehicleUpdated -= OnVehicleUpdated;
        _vm.OnStopStatusChanged -= OnStopStatusChanged;
        _vm.OnStopsLoaded -= OnStopsLoaded;
        _vm.OnSeekRoute -= OnSeekRoute;
        _vm.OnLogScrollRequested -= OnLogScrollRequested;
        _vm.PropertyChanged -= OnVmPropertyChanged;
    }
}
