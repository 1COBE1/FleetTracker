using Microsoft.AspNetCore.Mvc;
using FleetTracker.API.Services;

namespace FleetTracker.API.Controllers;

[ApiController]
[Route("api/vehicles")]
public class VehiclesController : ControllerBase
{
    private readonly VehicleService _vehicleService;

    public VehiclesController(VehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var vehicles = await _vehicleService.GetAllVehiclesAsync();
        return Ok(vehicles);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterVehicleRequest request)
    {
        var vehicleId = await _vehicleService.RegisterVehicleAsync(request.LicensePlate, request.Model);
        return Ok(new { vehicleId });


    }
}

public record RegisterVehicleRequest(string LicensePlate, string Model);
