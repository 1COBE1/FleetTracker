using MAUI.Models;
using MAUI.ViewModels;

namespace MAUI.Views;

public partial class TripDetailsPage : ContentPage, IQueryAttributable
{
    private readonly TripDetailsViewModel _vm;
    private bool _mapReady = false;

    public TripDetailsPage(TripDetailsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        _vm.OnStopsLoaded += OnStopsLoaded;
        _vm.OnVehicleUpdated += OnVehicleUpdated;
        _vm.OnStopStatusChanged += OnStopStatusChanged;

        TripMapWebView.Navigated += (s, e) =>
        {
            _mapReady = true;
            // Push stops that loaded before the map was ready
            foreach (var stop in _vm.Stops)
                PushStopToMap(stop);
            TripMapWebView.EvaluateJavaScriptAsync("fitToStops()");
        };
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("tripId", out var value) && Guid.TryParse(value?.ToString(), out var tripId))
            await _vm.LoadTripAsync(tripId);
    }

    private void OnStopsLoaded(IEnumerable<TripStopModel> stops)
    {
        if (!_mapReady) return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            foreach (var stop in stops)
                PushStopToMap(stop);
            TripMapWebView.EvaluateJavaScriptAsync("fitToStops()");
        });
    }

    private void OnVehicleUpdated(double latitude, double longitude)
    {
        if (!_mapReady) return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var js = $"updateVehicle({latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)});";
            TripMapWebView.EvaluateJavaScriptAsync(js);
        });
    }

    private void OnStopStatusChanged(Guid stopId, string status)
    {
        if (!_mapReady) return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var js = $"updateStopStatus('{stopId}', '{status}');";
            TripMapWebView.EvaluateJavaScriptAsync(js);
        });
    }

    private void PushStopToMap(TripStopModel stop)
    {
        var lat = stop.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var lng = stop.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var name = stop.Name.Replace("'", "\\'");
        var js = $"addStop('{stop.StopId}', {lat}, {lng}, '{name}', '{stop.Status}');";
        TripMapWebView.EvaluateJavaScriptAsync(js);
    }

    private async void OnReplayClicked(object sender, EventArgs e)
    {
        if (_vm.Trip is null) return;
        await Shell.Current.GoToAsync($"tripreplay?tripId={_vm.Trip.TripId}");
    }
}
