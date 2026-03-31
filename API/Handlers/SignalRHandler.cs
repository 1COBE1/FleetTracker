using FleetTracker.Application.DTOs;
using FleetTracker.Domain.Notifications;
using FleetTracker.Domain.Events;
using FleetTracker.Infrastructure.Repositories;
using FleetTracker.API.Hubs;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace FleetTracker.API.Handlers;

public class SignalRHandler : INotificationHandler<EventPersisted>
{
    private readonly IHubContext<FleetHub> _hub;
    private readonly TripRepository _tripRepo;

    public SignalRHandler(IHubContext<FleetHub> hub, TripRepository tripRepo)
    {
        _hub = hub;
        _tripRepo = tripRepo;
    }

    public async Task Handle(EventPersisted notification, CancellationToken ct)
    {
        switch (notification.Event)
        {
            case LocationUpdated e:
                await _hub.Clients.All.SendAsync("FleetUpdate",
                    new FleetUpdateDto(e.VehicleId, e.Latitude, e.Longitude, e.Speed, e.TripId), ct);
                break;

            case StopCompleted e:
                await _hub.Clients.All.SendAsync("StopCompleted",
                    new StopCompletedDto(e.TripId, e.StopId, e.StopName, e.ArrivedAt), ct);
                break;

            case HarshBrakeReported e:
                await _hub.Clients.All.SendAsync("SafetyAlert",
                    new SafetyAlertDto(e.VehicleId, e.VehicleName, e.Latitude, e.Longitude, e.DecelerationG, e.Timestamp), ct);
                break;

            // For trip-level status changes, read the full model from the projection
            // Safe: ReadModelHandler has already written the row (sequential publish)
            case TripStarted e:
                var startedTrip = await _tripRepo.GetTripByIdAsync(e.TripId);
                if (startedTrip != null)
                    await _hub.Clients.All.SendAsync("TripStatusChanged", (object)MapTripToPayload(startedTrip), ct);
                break;

            case TripEnded e:
                var endedTrip = await _tripRepo.GetTripByIdAsync(e.TripId);
                if (endedTrip != null)
                    await _hub.Clients.All.SendAsync("TripStatusChanged", (object)MapTripToPayload(endedTrip), ct);
                break;

        }
    }

    private static object MapTripToPayload(dynamic t) => new
    {
        TripId = (Guid)t.TripId,
        VehicleId = (Guid)t.VehicleId,
        VehicleName = (string)t.VehicleName,
        DriverName = (string)t.DriverName,
        Status = (string)t.Status,
        StartedAt = (DateTime)t.StartedAt,
        EndedAt = t.EndedAt is DateTime eda ? (DateTime?)eda : null,
        EstimatedArrival = t.EstimatedArrival is DateTime eta ? (DateTime?)eta : null,
        DistanceTraveled = ToDouble(t.DistanceTraveled),
        CurrentSpeed = ToDouble(t.CurrentSpeed),
        StopsTotal = ToInt(t.StopsTotal),
        StopsCompleted = ToInt(t.StopsCompleted),
        AlertsCount = ToInt(t.AlertsCount)
    };

    private static double ToDouble(object v) => v == null || v is DBNull ? 0.0 : Convert.ToDouble(v);
    private static int ToInt(object v) => v == null || v is DBNull ? 0 : Convert.ToInt32(v);
}
