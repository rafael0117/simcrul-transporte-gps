using Microsoft.AspNetCore.SignalR;
using SIMCRUL.Business.Interfaces;

namespace SIMCRUL.Business.Hubs;

public class GpsHub : Hub
{
    // Hub for broadcasting GPS telemetries and Alerts in real time.
    // Dashboard clients will listen to "SendTelemetryUpdate" and "SendAlertNotification".

    public async Task JoinDashboardGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "DashboardClients");
    }

    public async Task LeaveDashboardGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "DashboardClients");
    }
}
