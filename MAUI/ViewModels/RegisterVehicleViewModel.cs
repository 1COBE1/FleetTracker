using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MAUI.Services;
//using static Android.Graphics.ColorSpace;

namespace MAUI.ViewModels;

public partial class RegisterVehicleViewModel : ObservableObject
{
    private readonly VehicleService _vehicleService;

    [ObservableProperty]
    private string licensePlate = string.Empty;

    [ObservableProperty]
    private string model = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    public RegisterVehicleViewModel(VehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(LicensePlate))
        {
            await Shell.Current.DisplayAlert("Validation", "Enter a license plate.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(Model))
        {
            await Shell.Current.DisplayAlert("Validation", "Enter a vehicle model.", "OK");
            return;
        }

        IsLoading = true;
        try
        {
            await _vehicleService.RegisterVehicleAsync(LicensePlate, Model);
            await Shell.Current.DisplayAlert("Success", "Vehicle registered successfully!", "OK");
            LicensePlate = string.Empty;
            Model = string.Empty;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
