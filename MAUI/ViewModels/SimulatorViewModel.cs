using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MAUI.Models;
using MAUI.Services;
using System.Collections.ObjectModel;

namespace MAUI.ViewModels;

public partial class SimulatorViewModel : ObservableObject, IDisposable
{
    private readonly TripService _tripService;
    private readonly SimulatorService _simulatorService;

    [ObservableProperty]
    private ObservableCollection<TripModel> activeTrips = new();

    [ObservableProperty]
    private TripModel? selectedTrip;

    [ObservableProperty]
    private int speedMultiplier = 5;

    [ObservableProperty]
    private bool isRunning;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusText = "Select a trip to simulate";

    [ObservableProperty]
    private double currentLatitude;

    [ObservableProperty]
    private double currentLongitude;

    [ObservableProperty]
    private double currentSpeed; // NEW

    [ObservableProperty]
    private int waypointProgress;

    [ObservableProperty]
    private int waypointTotal;

    public double WaypointRatio => WaypointTotal == 0 ? 0 : (double)WaypointProgress / WaypointTotal;

    partial void OnWaypointProgressChanged(int value)
    => OnPropertyChanged(nameof(WaypointRatio));

    partial void OnWaypointTotalChanged(int value)
        => OnPropertyChanged(nameof(WaypointRatio));

    // NEW — consumed by SimulatorPage to push to map
    public event Action<SimulatorWaypoint>? OnPositionChanged;
    

    public bool IsNotRunning => !IsRunning;

    partial void OnIsRunningChanged(bool value)
        => OnPropertyChanged(nameof(IsNotRunning));

    public SimulatorViewModel(TripService tripService, SimulatorService simulatorService)
    {
        _tripService = tripService;
        _simulatorService = simulatorService;

        _simulatorService.OnPositionChanged += OnSimulatorPositionChanged;
        _simulatorService.OnSimulationCompleted += OnSimulationCompleted;
    }

    [RelayCommand]
    public async Task LoadTripsAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _tripService.GetActiveTripsAsync();
            ActiveTrips = new ObservableCollection<TripModel>(result);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (SelectedTrip is null)
        {
            await Shell.Current.DisplayAlert("Validation", "Select a trip first.", "OK");
            return;
        }

        IsLoading = true;
        try
        {
            var stops = await _tripService.GetStopsAsync(SelectedTrip.TripId);

            if (stops.Count < 2)
            {
                await Shell.Current.DisplayAlert("Validation", "Trip needs at least 2 stops to simulate.", "OK");
                return;
            }

            _simulatorService.Initialize(SelectedTrip, stops);

            WaypointTotal = stops.Count * 30;
            WaypointProgress = 0;
            StatusText = $"Ready — {stops.Count} stops, {WaypointTotal} waypoints";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task StartAsync()
    {
        if (_simulatorService.IsRunning) return;

        if (SelectedTrip is null)
        {
            await Shell.Current.DisplayAlert("Validation", "Select and initialize a trip first.", "OK");
            return;
        }

        IsRunning = true;
        StatusText = $"Simulating at {SpeedMultiplier}x speed...";

        _ = Task.Run(async () =>
        {
            await _simulatorService.StartAsync(SpeedMultiplier);
        });
    }

    [RelayCommand]
    private void Stop()
    {
        _simulatorService.Stop();
        IsRunning = false;
        StatusText = "Stopped";
    }

    [RelayCommand]
    private void Reset()
    {
        _simulatorService.Reset();
        IsRunning = false;
        WaypointProgress = 0;
        StatusText = "Reset — ready to start again";
    }

    // Renamed to avoid conflict with public event
    private void OnSimulatorPositionChanged(SimulatorWaypoint waypoint)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CurrentLatitude = waypoint.Latitude;
            CurrentLongitude = waypoint.Longitude;
            CurrentSpeed = waypoint.Speed; // NEW
            WaypointProgress++;
            StatusText = $"Waypoint {WaypointProgress} / {WaypointTotal}";

            // Fire public event for the page to consume
            OnPositionChanged?.Invoke(waypoint);
        });
    }

    private void OnSimulationCompleted()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsRunning = false;
            StatusText = "✅ Simulation complete — all stops visited";
        });
    }
    public void Dispose()
    {
        _simulatorService.OnPositionChanged -= OnSimulatorPositionChanged;
        _simulatorService.OnSimulationCompleted -= OnSimulationCompleted;
    }
}
