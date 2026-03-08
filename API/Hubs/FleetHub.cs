using Microsoft.AspNetCore.SignalR;

namespace FleetTracker.API.Hubs;

public class FleetHub : Hub
{
    public async Task SubscribeToFleet()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "fleet-all");
    }

    public async Task UnsubscribeFromFleet()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "fleet-all");
    }

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "fleet-all");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "fleet-all");
        await base.OnDisconnectedAsync(exception);
    }
}
