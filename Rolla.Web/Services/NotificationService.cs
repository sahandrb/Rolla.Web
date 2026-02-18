using Microsoft.AspNetCore.SignalR;
using Rolla.Application.Interfaces;
using Rolla.Web.Hubs;

namespace Rolla.Web.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<RideHub> _hubContext;

    public NotificationService(IHubContext<RideHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyNewTripAsync(IEnumerable<string> driverIds, int tripId, double lat, double lng, decimal price)
    {
        foreach (var driverId in driverIds)
        {
            await _hubContext.Clients.Group($"User_{driverId}").SendAsync("ReceiveTripOffer", new
            {
                tripId = tripId,
                price = price,
                originLat = lat,
                originLng = lng
            });
        }
    }

    public async Task NotifyTripAcceptedAsync(int tripId, string riderId, string driverId)
    {
        await _hubContext.Clients.Group($"User_{riderId}").SendAsync("TripAccepted", new
        {
            TripId = tripId,
            DriverId = driverId,
            Message = "راننده سفر شما را پذیرفت!"
        });
    }

    public async Task NotifyStatusChangeAsync(int tripId, string message)
    {
        await _hubContext.Clients.Group($"Trip_{tripId}").SendAsync("ReceiveStatusUpdate", message);
    }

    public async Task NotifyDriverLocationToRiderAsync(int tripId, double lat, double lng)
    {
        await _hubContext.Clients.Group($"Trip_{tripId}").SendAsync("ReceiveDriverLocation", lat, lng);
    }

    public async Task NotifyDriverAsync(string driverId, int tripId, double lat, double lng, decimal price)
    {
        await _hubContext.Clients.Group($"User_{driverId}").SendAsync("ReceiveTripOffer", new
        {
            tripId = tripId,
            price = price,
            originLat = lat,
            originLng = lng
        });
    }
}