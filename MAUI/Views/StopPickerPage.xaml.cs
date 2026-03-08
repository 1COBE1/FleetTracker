using MAUI.Services;
using System.Globalization;

namespace MAUI.Views;

public partial class StopPickerPage : ContentPage
{
    private readonly StopPickerService _stopPickerService;

    public StopPickerPage(StopPickerService stopPickerService)
    {
        InitializeComponent();
        _stopPickerService = stopPickerService;
    }

    private async void OnWebViewNavigating(object sender, WebNavigatingEventArgs e)
    {
        if (!e.Url.StartsWith("stoppicker://picked")) return;

        e.Cancel = true;

        try
        {
            var uri = new Uri(e.Url);
            var queryParams = uri.Query.TrimStart('?')
                .Split('&')
                .Select(p => p.Split('=', 2))
                .ToDictionary(p => p[0], p => Uri.UnescapeDataString(p[1]));

            var lat = double.Parse(queryParams["lat"], CultureInfo.InvariantCulture);
            var lng = double.Parse(queryParams["lng"], CultureInfo.InvariantCulture);
            var address = queryParams.TryGetValue("address", out var addr)
                          ? addr
                          : $"{lat:F5}, {lng:F5}";

            _stopPickerService.NotifyLocationPicked(lat, lng, address);
        }
        catch { /* ignore malformed URLs */ }

        await Shell.Current.GoToAsync("..");
    }
}
