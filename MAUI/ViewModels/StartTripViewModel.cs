using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MAUI.Models;
using MAUI.Services;
using System.Collections.ObjectModel;

namespace MAUI.ViewModels;

public partial class StartTripViewModel : ObservableObject
{
    private readonly TripService _tripService;
    private readonly VehicleService _vehicleService;
    private readonly StopPickerService _stopPickerService;

    [ObservableProperty]
    private ObservableCollection<VehicleModel> vehicles = new();

    [ObservableProperty]
    private VehicleModel? selectedVehicle;

    [ObservableProperty]
    private string driverName = string.Empty;

    [ObservableProperty]
    private string newStopName = string.Empty;

    [ObservableProperty]
    private ObservableCollection<StopInputModel> stops = new();

    [ObservableProperty]
    private bool isLoading;

    public StartTripViewModel(TripService tripService, VehicleService vehicleService, StopPickerService stopPickerService)
    {
        _tripService = tripService;
        _vehicleService = vehicleService;
        _stopPickerService = stopPickerService;

        _stopPickerService.OnLocationPicked += OnLocationPicked;
    }

    [RelayCommand]
    public async Task LoadVehiclesAsync()
    {
        var result = await _vehicleService.GetAllAsync();
        Vehicles = new ObservableCollection<VehicleModel>(result);
    }

    [RelayCommand]
    private async Task PickStopLocationAsync()
    {
        if (string.IsNullOrWhiteSpace(NewStopName))
        {
            await Shell.Current.DisplayAlert("Validation", "Enter a stop name before picking a location.", "OK");
            return;
        }
        await Shell.Current.GoToAsync("stoppicker");
    }

    [RelayCommand]
    private void RemoveStop(StopInputModel stop) => Stops.Remove(stop);

    [RelayCommand]
    private async Task StartTripAsync()
    {
        if (SelectedVehicle is null)
        {
            await Shell.Current.DisplayAlert("Validation", "Select a vehicle.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(DriverName))
        {
            await Shell.Current.DisplayAlert("Validation", "Enter a driver name.", "OK");
            return;
        }
        if (Stops.Count == 0)
        {
            await Shell.Current.DisplayAlert("Validation", "Add at least one stop.", "OK");
            return;
        }

        IsLoading = true;
        try
        {
            var tripId = await _tripService.StartTripAsync(
                SelectedVehicle.VehicleId,
                SelectedVehicle.VehicleName,
                DriverName,
                Stops.ToList());

            await Shell.Current.DisplayAlert("Success", "Trip started!", "OK");
            await Shell.Current.GoToAsync($"tripdetails?tripId={tripId}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnLocationPicked(double latitude, double longitude, string address)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Stops.Add(new StopInputModel
            {
                Name = NewStopName,
                Address = address,
                Latitude = latitude,
                Longitude = longitude
            });
            NewStopName = string.Empty;
        });
    }
}
