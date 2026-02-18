using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Rolla.Application.DTOs.Trip;
using Rolla.Application.Interfaces;
using Rolla.Domain.Entities;
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

    public async Task<int> CreateTripAsync(CreateTripDto dto, string riderId)
    {
        // ... کدهای قبلی ذخیره سفر ...
        _context.Trips.Add(trip);
        await _context.SaveChangesAsync();

        // ✅ منطق بیزنس اینجاست: رانندگان نزدیک را پیدا کن (مثلاً در شعاع ۵ کیلومتری)
        var nearbyDrivers = await _geoLocationService.GetNearbyDriversAsync(dto.OriginLat, dto.OriginLng, 5);

        // ✅ حالا به نوتیفیکیشن بگو فقط به این‌ها خبر بده
        await _notificationService.NotifyNewTripAsync(nearbyDrivers, trip.Id, dto.OriginLat, dto.OriginLng, trip.Price);

        return trip.Id;
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
    public async Task NotifyStatusChangeAsync(int tripId, string message)
    {
        // تغییر استراتژی: ارسال به گروه Trip_{tripId}
        // اینطوری هم مسافر، هم راننده و هم سیستم مانیتورینگ ادمین پیام را می‌گیرند
        await _hubContext.Clients.Group($"Trip_{tripId}").SendAsync("ReceiveStatusUpdate", message);
    }

    public async Task NotifyDriverAsync(string driverId, int tripId, double lat, double lng, decimal price)
    {
        // ارسال مستقیم به گروه اختصاصی راننده
        await _hubContext.Clients.Group($"User_{driverId}").SendAsync("ReceiveTripOffer", new
        {
            tripId = tripId, // حتماً با حروف کوچک شروع شود تا در JS راحت‌تر خوانده شود
            price = price,
            originLat = lat,
            originLng = lng
        });
    }
    // به انتهای کلاس اضافه کن
    public async Task NotifyDriverLocationToRiderAsync(int tripId, double lat, double lng)
    {
        // ارسال به گروه سفر (Rider و Driver در این گروه عضو هستند)
        // نام متد کلاینت: "ReceiveDriverLocation" (باید با rider-logic.js هماهنگ باشد)
        await _hubContext.Clients.Group($"Trip_{tripId}").SendAsync("ReceiveDriverLocation", lat, lng);
    }
}