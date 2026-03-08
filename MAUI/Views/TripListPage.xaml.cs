using MAUI.Models;
using MAUI.ViewModels;

namespace MAUI.Views;

public partial class TripListPage : ContentPage
{
    private readonly TripListViewModel _vm;

    public TripListPage(TripListViewModel vm)
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

    private async void OnTripSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not TripModel trip) return;
        await Shell.Current.GoToAsync($"tripdetails?tripId={trip.TripId}");
    }
}
