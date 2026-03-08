using System.Net.Http.Json;
using MAUI.Models;

namespace MAUI.Services;

public class VehicleService
{
    private readonly HttpClient _http;

    public VehicleService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<VehicleModel>> GetAllAsync()
    {
        return await _http.GetFromJsonAsync<List<VehicleModel>>("/api/vehicles")
               ?? new List<VehicleModel>();
    }

    public async Task<Guid> RegisterVehicleAsync(string licensePlate, string model)
    {
        var body = new { LicensePlate = licensePlate, Model = model };
        var response = await _http.PostAsJsonAsync("/api/vehicles/register", body);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<VehicleIdResponse>();
        return result!.VehicleId;
    }
}

file record VehicleIdResponse(Guid VehicleId);
