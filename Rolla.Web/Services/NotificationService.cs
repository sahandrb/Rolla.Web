using Microsoft.AspNetCore.SignalR;
using Rolla.Application.Interfaces; // حتما این یوزینگ رو چک کن
using Rolla.Web.Hubs;

namespace Rolla.Web.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<RideHub> _hubContext;

    public NotificationService(IHubContext<RideHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyNewTripAsync(int tripId, double lat, double lng, decimal price)
    {
        // ارسال پیام زنده به همه (ReceiveNewTripRequest همون اسمیه که تو JS نوشتی)
        await _hubContext.Clients.All.SendAsync("ReceiveNewTripRequest", new
        {
            TripId = tripId,
            Lat = lat,
            Lng = lng,
            Price = price
        });
    }
}