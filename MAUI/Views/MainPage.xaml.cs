using MAUI.Models;
using MAUI.ViewModels;

namespace MAUI.Views;

public partial class MainPage : ContentPage
{
    private readonly FleetMapViewModel _vm;

    public MainPage(FleetMapViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        _vm.OnVehicleUpdated += UpdateVehicleOnMap;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadVehiclesAsync();

        // Load initial vehicles onto map after data loads
        foreach (var vehicle in _vm.Vehicles)
            await PushVehicleToMap(vehicle);
    }

    private async void UpdateVehicleOnMap(VehicleModel vehicle)
    {
        await PushVehicleToMap(vehicle);
    }

    private async Task PushVehicleToMap(VehicleModel vehicle)
    {
        var js = $"updateVehicle('{vehicle.VehicleId}', {vehicle.Latitude}, {vehicle.Longitude}, '{vehicle.VehicleName}', {vehicle.Speed});";
        await MainThread.InvokeOnMainThreadAsync(() =>
            FleetMapWebView.EvaluateJavaScriptAsync(js));
    }
}
