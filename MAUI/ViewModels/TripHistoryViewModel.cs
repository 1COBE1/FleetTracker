using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MAUI.Models;
using MAUI.Services;
using System.Collections.ObjectModel;

namespace MAUI.ViewModels;

public partial class TripHistoryViewModel : ObservableObject
{
    private readonly TripService _tripService;

    [ObservableProperty]
    private ObservableCollection<TripModel> completedTrips = new();

    [ObservableProperty]
    private bool isLoading;

    public TripHistoryViewModel(TripService tripService)
    {
        _tripService = tripService;
    }

    [RelayCommand]
    public async Task LoadTripsAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _tripService.GetCompletedTripsAsync();
            CompletedTrips = new ObservableCollection<TripModel>(result);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
