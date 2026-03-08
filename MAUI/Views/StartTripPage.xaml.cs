using MAUI.ViewModels;

namespace MAUI.Views;

public partial class StartTripPage : ContentPage
{
    private readonly StartTripViewModel _vm;

    public StartTripPage(StartTripViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadVehiclesAsync();
    }
}
