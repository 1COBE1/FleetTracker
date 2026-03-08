using System.Net.Http.Json;
using MAUI.Models;

namespace MAUI.Services;

public class AlertService
{
    private readonly HttpClient _http;

    public AlertService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<SafetyAlertModel>> GetTripAlertsAsync(Guid tripId)
    {
        return await _http.GetFromJsonAsync<List<SafetyAlertModel>>($"/api/trips/{tripId}/alerts")
               ?? new List<SafetyAlertModel>();
    }
}
