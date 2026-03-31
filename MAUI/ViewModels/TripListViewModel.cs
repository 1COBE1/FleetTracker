using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MAUI.Models;
using MAUI.Services;
using System.Collections.ObjectModel;

namespace MAUI.ViewModels;

public partial class TripListViewModel : ObservableObject, IDisposable
{
    private readonly TripService _tripService;
    private readonly FleetSignalRService _signalR;

    [ObservableProperty]
    private ObservableCollection<TripModel> activeTrips = new();

    [ObservableProperty]
    private bool isLoading;

    public TripListViewModel(TripService tripService, FleetSignalRService signalR)
    {
        _tripService = tripService;
        _signalR = signalR;

        _signalR.OnTripStatusChanged += OnTripStatusChanged;
    }

    [RelayCommand]
    public async Task LoadTripsAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _tripService.GetActiveTripsAsync();
            ActiveTrips = new ObservableCollection<TripModel>(result);
            await _signalR.ConnectAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnTripStatusChanged(TripModel trip)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var existing = ActiveTrips.FirstOrDefault(t => t.TripId == trip.TripId);
            if (existing is not null)
            {
                var index = ActiveTrips.IndexOf(existing);
                ActiveTrips[index] = trip;
            }
            else
            {
                ActiveTrips.Add(trip);
            }
        });
    }
    public void Dispose()
    {
        _signalR.OnTripStatusChanged -= OnTripStatusChanged;
    }
}
