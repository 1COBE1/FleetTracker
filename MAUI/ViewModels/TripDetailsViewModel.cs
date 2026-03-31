using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MAUI.Models;
using MAUI.Services;
using System.Collections.ObjectModel;

namespace MAUI.ViewModels;

public partial class TripDetailsViewModel : ObservableObject, IDisposable
{
    private readonly TripService _tripService;
    private readonly AlertService _alertService;
    private readonly FleetSignalRService _signalR;

    [ObservableProperty]
    private TripModel? trip;

    [ObservableProperty]
    private ObservableCollection<TripStopModel> stops = new();

    [ObservableProperty]
    private ObservableCollection<SafetyAlertModel> alerts = new();

    [ObservableProperty]
    private bool isLoading;

    // NEW — events for the map
    public event Action<IEnumerable<TripStopModel>>? OnStopsLoaded;
    public event Action<double, double>? OnVehicleUpdated;
    public event Action<Guid, string>? OnStopStatusChanged;

    public TripDetailsViewModel(TripService tripService, AlertService alertService, FleetSignalRService signalR)
    {
        _tripService = tripService;
        _alertService = alertService;
        _signalR = signalR;

        _signalR.OnSafetyAlert += OnSafetyAlert;
        _signalR.OnTripStatusChanged += OnTripStatusChanged;
        _signalR.OnStopApproaching += OnStopApproaching;
        _signalR.OnStopCompleted += OnStopCompleted;
        _signalR.OnLocationUpdated += OnLocationUpdated; // NEW
    }

    [RelayCommand]
    public async Task LoadTripAsync(Guid tripId)
    {
        IsLoading = true;
        try
        {
            Trip = await _tripService.GetByIdAsync(tripId);
            var stopList = await _tripService.GetStopsAsync(tripId);
            var alertList = await _alertService.GetTripAlertsAsync(tripId);

            Stops = new ObservableCollection<TripStopModel>(stopList);
            Alerts = new ObservableCollection<SafetyAlertModel>(alertList);

            // Fire map event after stops are loaded
            OnStopsLoaded?.Invoke(Stops);

            await _signalR.ConnectAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task EndTripAsync()
    {
        if (Trip is null) return;

        var confirm = await Shell.Current.DisplayAlert(
            "End Trip",
            $"End trip for {Trip.VehicleName}?",
            "Yes", "Cancel");

        if (!confirm) return;

        IsLoading = true;
        try
        {
            await _tripService.EndTripAsync(Trip.TripId);
            Trip = await _tripService.GetByIdAsync(Trip.TripId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnSafetyAlert(SafetyAlertModel alert)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (alert.TripId == Trip?.TripId)
                Alerts.Insert(0, alert);
        });
    }

    private void OnTripStatusChanged(TripModel updated)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (updated.TripId == Trip?.TripId)
                Trip = updated;
        });
    }

    // NEW — push vehicle position to map
    private void OnLocationUpdated(LocationUpdateModel update)
    {
        if (update.TripId != Trip?.TripId) return;
        OnVehicleUpdated?.Invoke(update.Latitude, update.Longitude);
    }

    private void OnStopApproaching(StopApproachingModel model)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (model.TripId != Trip?.TripId) return;

            var stop = Stops.FirstOrDefault(s => s.StopId == model.StopId);
            if (stop is null) return;

            var index = Stops.IndexOf(stop);
            stop.Status = "Active";
            Stops[index] = stop;

            // NEW — update map marker
            OnStopStatusChanged?.Invoke(model.StopId, "Active");
        });
    }

    private async void OnStopCompleted(StopCompletedModel model)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (model.TripId != Trip?.TripId) return;

            var stop = Stops.FirstOrDefault(s => s.StopId == model.StopId);
            if (stop is not null)
            {
                var index = Stops.IndexOf(stop);
                stop.Status = "Completed";
                stop.ActualArrival = model.ArrivedAt;
                Stops[index] = stop;

                // NEW — update map marker
                OnStopStatusChanged?.Invoke(model.StopId, "Completed");
            }

            Trip = await _tripService.GetByIdAsync(model.TripId);
        });
    }

    public void Dispose()
    {
        _signalR.OnSafetyAlert -= OnSafetyAlert;
        _signalR.OnTripStatusChanged -= OnTripStatusChanged;
        _signalR.OnStopApproaching -= OnStopApproaching;
        _signalR.OnStopCompleted -= OnStopCompleted;
        _signalR.OnLocationUpdated -= OnLocationUpdated;
    }
}
