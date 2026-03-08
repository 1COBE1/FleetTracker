using MAUI.Models;
using MAUI.ViewModels;
using System.Globalization;

namespace MAUI.Views;

public partial class SimulatorPage : ContentPage
{
    private readonly SimulatorViewModel _vm;
    private bool _mapReady = false;

    public SimulatorPage(SimulatorViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        _vm.OnPositionChanged += OnPositionChanged;
        SimMapWebView.Navigated += (s, e) => _mapReady = true;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadTripsAsync();
    }

    private void OnPositionChanged(SimulatorWaypoint waypoint)
    {
        if (!_mapReady) return;

        var lat = waypoint.Latitude.ToString(CultureInfo.InvariantCulture);
        var lng = waypoint.Longitude.ToString(CultureInfo.InvariantCulture);
        var name = (_vm.SelectedTrip?.VehicleName ?? "Vehicle").Replace("'", "\\'");
        var speed = waypoint.Speed.ToString(CultureInfo.InvariantCulture);
        var id = _vm.SelectedTrip?.VehicleId.ToString() ?? "";

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var js = $"updateVehicle('{id}', {lat}, {lng}, '{name}', {speed});";
            SimMapWebView.EvaluateJavaScriptAsync(js);
        });
    }
}
