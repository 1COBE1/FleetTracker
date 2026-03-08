using CommunityToolkit.Maui;
using MAUI.Services;
using MAUI.ViewModels;
using MAUI.Views;
using Microsoft.Extensions.Logging;

using MAUI;

namespace MAUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Backend URL - change this to your machine's IP when testing on device
        var backendUrl = "http://localhost:5108";

        // Services (Singleton - one instance for app lifetime)
        builder.Services.AddHttpClient<VehicleService>(client =>
            client.BaseAddress = new Uri(backendUrl));

        builder.Services.AddHttpClient<TripService>(client =>
            client.BaseAddress = new Uri(backendUrl));

        builder.Services.AddHttpClient<AlertService>(client =>
            client.BaseAddress = new Uri(backendUrl));

        builder.Services.AddSingleton(new FleetSignalRService($"{backendUrl}/fleetHub"));

        // ViewModels (Transient - new instance per page)
        builder.Services.AddTransient<FleetMapViewModel>();
        builder.Services.AddTransient<TripListViewModel>();
        builder.Services.AddTransient<TripDetailsViewModel>();

        // Pages (Transient - new instance per navigation)
        builder.Services.AddTransient<Views.MainPage>();
        builder.Services.AddTransient<TripListPage>();
        builder.Services.AddTransient<TripDetailsPage>();

        builder.Services.AddSingleton<StopPickerService>();

        builder.Services.AddTransient<RegisterVehicleViewModel>();
        builder.Services.AddTransient<RegisterVehiclePage>();
        builder.Services.AddTransient<StartTripViewModel>();
        builder.Services.AddTransient<StartTripPage>();
        builder.Services.AddTransient<StopPickerPage>();

        // Register named client
        builder.Services.AddHttpClient("SimulatorClient", client =>
            client.BaseAddress = new Uri(backendUrl));

        // Keep as singleton
        builder.Services.AddSingleton<SimulatorService>();
        builder.Services.AddTransient<SimulatorViewModel>();
        builder.Services.AddTransient<SimulatorPage>();


#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
