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

    // این متد وقتی صدا زده میشه که مسافر دکمه "درخواست سفر" رو زده
    public async Task NotifyNewTripAsync(int tripId, double lat, double lng, decimal price)
    {
        // به همه راننده‌ها خبر بده (Broadcasting)
        // در نسخه نهایی، اینجا فقط به راننده‌های نزدیک (GeoQuery) خبر میدیم
        await _hubContext.Clients.All.SendAsync("ReceiveNewTripRequest", new
        {
            TripId = tripId,
            Lat = lat,
            Lng = lng,
            Price = price
        });
    }
}