using Microsoft.AspNetCore.SignalR;
using Rolla.Application.Interfaces;
using Rolla.Web.Hubs;

namespace Rolla.Web.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<RideHub> _hubContext;
    private readonly IGeoLocationService _geoService; // نیاز به Redis داریم

    public NotificationService(IHubContext<RideHub> hubContext, IGeoLocationService geoService)
    {
        _hubContext = hubContext;
        _geoService = geoService;
    }

    public async Task NotifyNewTripAsync(int tripId, double lat, double lng, decimal price)
    {
        // ۱. پیدا کردن رانندگان در شعاع ۱۰ کیلومتری از Redis
        var nearbyDriverIds = await _geoService.GetNearbyDriversAsync(lat, lng, 10);

        // ۲. ارسال پیام فقط به این رانندگان (با استفاده از User ID)
        foreach (var driverId in nearbyDriverIds)
        {
            // در RideHub ما هر کاربر را در گروهی به نام "User_{UserId}" عضو کردیم
            // یا می‌توانیم مستقیم از متد User استفاده کنیم اگر Identity درست وصل باشد
            await _hubContext.Clients.Group($"User_{driverId}").SendAsync("ReceiveTripOffer", new
            {
                TripId = tripId,
                Price = price,
                OriginLat = lat,
                OriginLng = lng
            });
        }
    }
    public async Task NotifyTripAcceptedAsync(int tripId, string riderId, string driverId)
    {
        // به مسافر خبر می‌دهیم (گروه User_RiderId)
        await _hubContext.Clients.Group($"User_{riderId}").SendAsync("TripAccepted", new
        {
            TripId = tripId,
            DriverId = driverId,
            Message = "راننده سفر شما را پذیرفت!"
        });
    }
}