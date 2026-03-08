using Microsoft.AspNetCore.Mvc;
using FleetTracker.API.Services;

namespace FleetTracker.API.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly LocationService _locationService;

    public DashboardController(LocationService locationService)
    {
        _locationService = locationService;
    }

    [HttpGet("live-map")]
    public async Task<IActionResult> GetLiveMap()
    {
        var positions = await _locationService.GetAllVehiclePositionsAsync();
        return Ok(positions);
    }

    [HttpGet("alerts")]
    public async Task<IActionResult> GetAlerts()
    {
        var alerts = await _locationService.GetSafetyAlertsAsync();
        return Ok(alerts);
    }
}
