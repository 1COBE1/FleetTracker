using MAUI.Models;
using MAUI.ViewModels;
using System.IO;

namespace MAUI.Views;

public partial class MainPage : ContentPage
{
    private readonly FleetMapViewModel _vm;
    private bool _isActive;
    private bool _isMapReady;

    public MainPage(FleetMapViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        _vm.OnVehicleUpdated += UpdateVehicleOnMap;

        FleetMapWebView.Navigated += OnMapNavigated;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isActive = true;

        await _vm.LoadVehiclesAsync();

        // Load initial vehicles onto map after data loads
        foreach (var vehicle in _vm.Vehicles)
            await PushVehicleToMap(vehicle);
    }

    protected override void OnDisappearing()
    {
        _isActive = false;
        base.OnDisappearing();
    }

    private async void UpdateVehicleOnMap(VehicleModel vehicle)
    {
        await PushVehicleToMap(vehicle);
    }

    private async Task PushVehicleToMap(VehicleModel vehicle)
    {
        var js = $"updateVehicle('{vehicle.VehicleId}', {vehicle.Latitude}, {vehicle.Longitude}, '{vehicle.VehicleName}', {vehicle.Speed});";

        var handlerPresent = FleetMapWebView?.Handler != null;
        var source = FleetMapWebView?.Source?.ToString() ?? "null";

#region agent log
        try
        {
            var logLine =
                $"{{\"sessionId\":\"845338\",\"runId\":\"post_fix\",\"hypothesisId\":\"H5\",\"location\":\"MainPage.xaml.cs:PushVehicleToMap\",\"message\":\"BeforeEvaluateJs\",\"data\":{{\"handlerPresent\":{handlerPresent.ToString().ToLower()},\"source\":\"{source}\",\"isActive\":{_isActive.ToString().ToLower()},\"isMapReady\":{_isMapReady.ToString().ToLower()}}},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}{Environment.NewLine}";
            File.AppendAllText("debug-845338.log", logLine);
        }
        catch
        {
        }
#endregion

        if (!_isActive || !_isMapReady)
        {
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                await FleetMapWebView.EvaluateJavaScriptAsync(js);
            }
            catch (Exception ex)
            {
#region agent log
                try
                {
                    var logLine =
                        $"{{\"sessionId\":\"845338\",\"runId\":\"post_fix\",\"hypothesisId\":\"H5\",\"location\":\"MainPage.xaml.cs:PushVehicleToMap\",\"message\":\"EvaluateJsException\",\"data\":{{\"exceptionType\":\"{ex.GetType().Name}\",\"handlerPresent\":{handlerPresent.ToString().ToLower()},\"source\":\"{source}\",\"isActive\":{_isActive.ToString().ToLower()},\"isMapReady\":{_isMapReady.ToString().ToLower()}}},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}{Environment.NewLine}";
                    File.AppendAllText("debug-845338.log", logLine);
                }
                catch
                {
                }
#endregion
                throw;
            }
        });
    }

    private void OnMapNavigated(object? sender, WebNavigatedEventArgs e)
    {
        _isMapReady = true;

#region agent log
        try
        {
            var logLine =
                $"{{\"sessionId\":\"845338\",\"runId\":\"post_fix\",\"hypothesisId\":\"H5\",\"location\":\"MainPage.xaml.cs:OnMapNavigated\",\"message\":\"MapNavigated\",\"data\":{{\"url\":\"{e.Url}\"}},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}{Environment.NewLine}";
            File.AppendAllText("debug-845338.log", logLine);
        }
        catch
        {
        }
#endregion
    }
}
