using MAUI.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace MAUI.Services;

public class FleetSignalRService
{
    private readonly HubConnection _connection;

    public event Action<LocationUpdateModel>? OnLocationUpdated;
    public event Action<SafetyAlertModel>? OnSafetyAlert;
    public event Action<TripModel>? OnTripStatusChanged;
    public event Action<StopApproachingModel>? OnStopApproaching; // NEW
    public event Action<StopCompletedModel>? OnStopCompleted;     // NEW

    public bool IsConnected => _connection.State == HubConnectionState.Connected;

    public FleetSignalRService(string hubUrl)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        // FIX: was "LocationUpdated", backend actually sends "FleetUpdate"
        _connection.On<LocationUpdateModel>("FleetUpdate", update =>
            OnLocationUpdated?.Invoke(update));

        _connection.On<SafetyAlertModel>("SafetyAlert", alert =>
            OnSafetyAlert?.Invoke(alert));

        _connection.On<TripModel>("TripStatusChanged", trip =>
            OnTripStatusChanged?.Invoke(trip));

        // NEW
        _connection.On<StopApproachingModel>("StopApproaching", model =>
            OnStopApproaching?.Invoke(model));

        // NEW
        _connection.On<StopCompletedModel>("StopCompleted", model =>
            OnStopCompleted?.Invoke(model));
    }

    public async Task ConnectAsync()
    {
        if (!IsConnected)
            await _connection.StartAsync();
    }

    public async Task DisconnectAsync()
    {
        if (IsConnected)
            await _connection.StopAsync();
    }
}
