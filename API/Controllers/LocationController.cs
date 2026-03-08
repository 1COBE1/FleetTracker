using Microsoft.AspNetCore.Mvc;
using FleetTracker.API.Services;

namespace FleetTracker.API.Controllers;

[ApiController]
[Route("api/location")]
public class LocationController : ControllerBase
{
    private readonly LocationService _locationService;

    public LocationController(LocationService locationService)
    {
        _locationService = locationService;
    }

    [HttpPost("update")]
    public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationRequest request)
    {
        await _locationService.UpdateLocationAsync(
            request.TripId,
            request.VehicleId,
            request.Latitude,
            request.Longitude,
            request.Speed,
            request.Heading);

        return Ok();
    }

    [HttpPost("harsh-brake")]
    public async Task<IActionResult> ReportHarshBrake([FromBody] HarshBrakeRequest request)
    {
        await _locationService.ReportHarshBrakeAsync(
            request.TripId,
            request.VehicleId,
            request.VehicleName,
            request.Latitude,
            request.Longitude,
            request.DecelerationG);

        return Ok();
    }
}

public record UpdateLocationRequest(
    Guid TripId,
    Guid VehicleId,
    double Latitude,
    double Longitude,
    double Speed,
    double Heading
);

public record HarshBrakeRequest(
    Guid TripId,
    Guid VehicleId,
    string VehicleName,
    double Latitude,
    double Longitude,
    double DecelerationG
);
