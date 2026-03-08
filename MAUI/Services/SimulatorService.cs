using System.Net.Http.Json;
using MAUI.Models;

namespace MAUI.Services;

public class SimulatorService
{
    private readonly HttpClient _http;
    private CancellationTokenSource? _cts;
    private SimulatorStateModel? _state;
    private readonly Random _random = new();

    // How many interpolated points between two stops
    private const int PointsBetweenStops = 30;

    public event Action<SimulatorWaypoint>? OnPositionChanged;
    public event Action? OnSimulationCompleted;

    public bool IsRunning => _state?.IsRunning ?? false;

    public SimulatorService(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient("SimulatorClient");
    }

    public void Initialize(TripModel trip, List<TripStopModel> stops)
    {
        var waypoints = GenerateWaypoints(stops);

        _state = new SimulatorStateModel
        {
            TripId = trip.TripId,
            VehicleId = trip.VehicleId,
            VehicleName = trip.VehicleName,
            Waypoints = waypoints,
            CurrentWaypointIndex = 0,
            IsRunning = false
        };
    }

    public async Task StartAsync(int speedMultiplier)
    {
        if (_state == null || _state.IsRunning) return;

        _state.IsRunning = true;
        _cts = new CancellationTokenSource();

        // Base interval: 1 second / multiplier
        var intervalMs = Math.Max(100, 1000 / speedMultiplier);

        try
        {
            while (_state.CurrentWaypointIndex < _state.Waypoints.Count
       && !_cts.Token.IsCancellationRequested)
            {
                var waypoint = _state.Waypoints[_state.CurrentWaypointIndex];

                double heading = 0;
                double speed = 40 + _random.NextDouble() * 40;

                if (_state.CurrentWaypointIndex > 0)
                {
                    var prev = _state.Waypoints[_state.CurrentWaypointIndex - 1];
                    heading = CalculateHeading(prev.Latitude, prev.Longitude,
                                               waypoint.Latitude, waypoint.Longitude);
                }

                await SendLocationUpdateAsync(waypoint, speed, heading);

                if (_random.NextDouble() < 0.05)
                    await SendHarshBrakeAsync(waypoint);

                OnPositionChanged?.Invoke(new SimulatorWaypoint
                {
                    Latitude = waypoint.Latitude,
                    Longitude = waypoint.Longitude,
                    Speed = speed  // pass the generated speed
                });

                _state.CurrentWaypointIndex++;

                try
                {
                    await Task.Delay(intervalMs, _cts.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }


            // All waypoints done
            OnSimulationCompleted?.Invoke();
        }
        catch (TaskCanceledException) { /* stopped manually */ }
        finally
        {
            _state.IsRunning = false;
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        if (_state != null)
            _state.IsRunning = false;
    }

    public void Reset()
    {
        Stop();
        if (_state != null)
            _state.CurrentWaypointIndex = 0;
    }

    public (double Latitude, double Longitude) GetCurrentPosition()
    {
        if (_state == null || _state.Waypoints.Count == 0)
            return (0, 0);

        var index = Math.Min(_state.CurrentWaypointIndex, _state.Waypoints.Count - 1);
        var waypoint = _state.Waypoints[index];
        return (waypoint.Latitude, waypoint.Longitude);
    }

    // Linear interpolation between all stops
    private static List<SimulatorWaypoint> GenerateWaypoints(List<TripStopModel> stops)
    {
        var waypoints = new List<SimulatorWaypoint>();
        if (stops.Count == 0) return waypoints;

        // Add starting point (first stop)
        waypoints.Add(new SimulatorWaypoint
        {
            Latitude = stops[0].Latitude,
            Longitude = stops[0].Longitude
        });

        for (int i = 0; i < stops.Count - 1; i++)
        {
            var from = stops[i];
            var to = stops[i + 1];

            // Interpolate PointsBetweenStops points between from and to
            for (int j = 1; j <= PointsBetweenStops; j++)
            {
                var t = (double)j / PointsBetweenStops;
                waypoints.Add(new SimulatorWaypoint
                {
                    Latitude = from.Latitude + t * (to.Latitude - from.Latitude),
                    Longitude = from.Longitude + t * (to.Longitude - from.Longitude)
                });
            }
        }

        return waypoints;
    }

    private async Task SendLocationUpdateAsync(SimulatorWaypoint waypoint, double speed, double heading)
    {
        if (_state == null) return;

        var body = new
        {
            TripId = _state.TripId,
            VehicleId = _state.VehicleId,
            Latitude = waypoint.Latitude,
            Longitude = waypoint.Longitude,
            Speed = speed,
            Heading = heading
        };

        var response = await _http.PostAsJsonAsync("/api/location/update", body);
        response.EnsureSuccessStatusCode();
    }

    private async Task SendHarshBrakeAsync(SimulatorWaypoint waypoint)
    {
        if (_state == null) return;

        var decelerationG = 0.6 + _random.NextDouble() * 0.6; // 0.6–1.2G

        var body = new
        {
            TripId = _state.TripId,
            VehicleId = _state.VehicleId,
            VehicleName = _state.VehicleName,
            Latitude = waypoint.Latitude,
            Longitude = waypoint.Longitude,
            DecelerationG = decelerationG
        };

        try
        {
            await _http.PostAsJsonAsync("/api/location/harsh-brake", body);
        }
        catch { /* silently ignore */ }
    }

    private static double CalculateHeading(double lat1, double lon1, double lat2, double lon2)
    {
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var lat1Rad = lat1 * Math.PI / 180;
        var lat2Rad = lat2 * Math.PI / 180;

        var x = Math.Sin(dLon) * Math.Cos(lat2Rad);
        var y = Math.Cos(lat1Rad) * Math.Sin(lat2Rad)
              - Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(dLon);

        var heading = Math.Atan2(x, y) * 180 / Math.PI;
        return (heading + 360) % 360;
    }
}
