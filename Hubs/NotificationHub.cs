using Microsoft.AspNetCore.SignalR;

namespace EasyCars.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task SendNotification(string userId, string message)
        {
            await Clients.User(userId).SendAsync("ReceiveNotification", message);
        }

        public async Task BroadcastStatusUpdate(string voitureId, string status)
        {
            await Clients.All.SendAsync("StatusUpdated", voitureId, status);
        }
    }
}
