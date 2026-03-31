using MAUI.ViewModels;

namespace MAUI.Views;

public partial class TripHistoryPage : ContentPage
{
    private readonly TripHistoryViewModel _vm;

    public TripHistoryPage(TripHistoryViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadTripsAsync();
    }

    private async void OnReplayClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Guid tripId)
            await Shell.Current.GoToAsync($"tripreplay?tripId={tripId}");
    }
}
