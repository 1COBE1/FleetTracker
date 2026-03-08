using Microsoft.AspNetCore.Mvc;
using FleetTracker.API.Services;
using FleetTracker.Domain.Events;

namespace FleetTracker.API.Controllers;

[ApiController]
[Route("api/trips")]
public class TripsController : ControllerBase
{
    private readonly TripService _tripService;

    public TripsController(TripService tripService)
    {
        _tripService = tripService;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveTrips()
    {
        var trips = await _tripService.GetActiveTripsAsync();
        return Ok(trips);
    }

    [HttpGet("{tripId}")]
    public async Task<IActionResult> GetTrip(Guid tripId)
    {
        var trip = await _tripService.GetTripByIdAsync(tripId);
        if (trip == null) return NotFound();
        return Ok(trip);
    }

    [HttpGet("{tripId}/stops")]
    public async Task<IActionResult> GetStops(Guid tripId)
    {
        var stops = await _tripService.GetStopsByTripIdAsync(tripId);
        return Ok(stops);
    }

    // Fix #1: this endpoint was missing — MAUI AlertService called it but got 404
    [HttpGet("{tripId}/alerts")]
    public async Task<IActionResult> GetAlerts(Guid tripId)
    {
        var alerts = await _tripService.GetAlertsByTripIdAsync(tripId);
        return Ok(alerts);
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartTrip([FromBody] StartTripRequest request)
    {
        var stops = request.Stops.Select((s, index) => new PlannedStop(
            Guid.NewGuid(),
            index + 1,
            s.Name,
            s.Address,
            s.Latitude,
            s.Longitude
        )).ToList();

        var tripId = await _tripService.StartTripAsync(
            request.VehicleId,
            request.VehicleName,
            request.DriverName,
            stops);

        return Ok(new { tripId });
    }

    [HttpPost("{tripId}/end")]
    public async Task<IActionResult> EndTrip(Guid tripId)
    {
        await _tripService.EndTripAsync(tripId);
        return Ok();
    }
}

public record StartTripRequest(
    Guid VehicleId,
    string VehicleName,
    string DriverName,
    List<StopRequest> Stops
);

public record StopRequest(
    string Name,
    string Address,
    double Latitude,
    double Longitude
);
