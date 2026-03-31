using System.Net.Http.Json;
using MAUI.Models;

namespace MAUI.Services;

public class TripService
{
    private readonly HttpClient _http;

    public TripService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<TripModel>> GetActiveTripsAsync()
    {
        return await _http.GetFromJsonAsync<List<TripModel>>("/api/trips/active")
               ?? new List<TripModel>();
    }

    public async Task<TripModel?> GetByIdAsync(Guid tripId)
    {
        return await _http.GetFromJsonAsync<TripModel>($"/api/trips/{tripId}");
    }

    public async Task<List<TripStopModel>> GetStopsAsync(Guid tripId)
    {
        return await _http.GetFromJsonAsync<List<TripStopModel>>($"/api/trips/{tripId}/stops")
               ?? new List<TripStopModel>();
    }

    public async Task<Guid> StartTripAsync(Guid vehicleId, string vehicleName, string driverName, List<StopInputModel> stops)
    {
        var body = new
        {
            VehicleId = vehicleId,
            VehicleName = vehicleName,
            DriverName = driverName,
            Stops = stops.Select(s => new
            {
                s.Name,
                s.Address,
                s.Latitude,
                s.Longitude
            }).ToList()
        };

        var response = await _http.PostAsJsonAsync("/api/trips/start", body);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TripIdResponse>();
        return result!.TripId;
    }

    public async Task EndTripAsync(Guid tripId)
    {
        var response = await _http.PostAsync($"/api/trips/{tripId}/end", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<TripModel>> GetCompletedTripsAsync()
    {
        return await _http.GetFromJsonAsync<List<TripModel>>("/api/trips/completed")
               ?? new List<TripModel>();
    }

    public async Task<TripReplayModel?> GetTripReplayAsync(Guid tripId)
    {
        return await _http.GetFromJsonAsync<TripReplayModel>($"/api/trips/{tripId}/replay");
    }
}

file record TripIdResponse(Guid TripId);
