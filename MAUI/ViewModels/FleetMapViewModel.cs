using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MAUI.Models;
using MAUI.Services;
using System.Collections.ObjectModel;

namespace MAUI.ViewModels;

public partial class FleetMapViewModel : ObservableObject, IDisposable
{
    private readonly VehicleService _vehicleService;
    private readonly FleetSignalRService _signalR;

    public event Action<VehicleModel>? OnVehicleUpdated;

    [ObservableProperty]
    public partial ObservableCollection<VehicleModel> Vehicles { get; set; } = new();

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    public FleetMapViewModel(VehicleService vehicleService, FleetSignalRService signalR)
    {
        _vehicleService = vehicleService;
        _signalR = signalR;
        _signalR.OnLocationUpdated += OnLocationUpdated;
    }

    [RelayCommand]
    public async Task LoadVehiclesAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _vehicleService.GetAllAsync();
            Vehicles = new ObservableCollection<VehicleModel>(result);
            await _signalR.ConnectAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToTripsAsync()
    {
        // Shell route: FlyoutItem "home" > TabBar > ShellContent "trips"
        await Shell.Current.GoToAsync("//home/trips");
    }

    private void OnLocationUpdated(LocationUpdateModel update)
    {
        var vehicle = Vehicles.FirstOrDefault(v => v.VehicleId == update.VehicleId);
        if (vehicle is null) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            vehicle.Latitude = update.Latitude;
            vehicle.Longitude = update.Longitude;
            vehicle.Speed = update.Speed;
            OnVehicleUpdated?.Invoke(vehicle);
        });
    }

    public void Dispose()
    {
        _signalR.OnLocationUpdated -= OnLocationUpdated;
    }
}
