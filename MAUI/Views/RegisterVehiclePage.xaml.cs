using MAUI.ViewModels;

namespace MAUI.Views;

public partial class RegisterVehiclePage : ContentPage
{
    public RegisterVehiclePage(RegisterVehicleViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
