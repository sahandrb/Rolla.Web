using Microsoft.AspNetCore.SignalR;
using Rolla.Application.Interfaces;
using System.Collections.Concurrent;

namespace Rolla.Web.Hubs;

public class RideHub : Hub
{
    // این لیست موقت رو فعلاً برای تست نگه می‌داریم تا ببینیم کی آنلاینه
    // (در آینده با Redis جایگزین میشه)
    private static readonly ConcurrentDictionary<string, string> _onlineUsers = new();

    // ---------------------------------------------------------
    // 1. وقتی کاربر (راننده یا مسافر) به سرور وصل میشه
    // ---------------------------------------------------------
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier; // آیدی کاربر از سیستم Identity
        var connectionId = Context.ConnectionId; // آیدی سوکت فعلی

        if (!string.IsNullOrEmpty(userId))
        {
            _onlineUsers[userId] = connectionId;
            // کاربر رو به گروه خودش اضافه می‌کنیم تا بتونیم بهش پیام خصوصی بدیم
            await Groups.AddToGroupAsync(connectionId, $"User_{userId}");
        }

        await base.OnConnectedAsync();
    }

    // ---------------------------------------------------------
    // 2. وقتی کاربر قطع میشه (اینترنتش میره یا اپ رو میبنده)
    // ---------------------------------------------------------
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            _onlineUsers.TryRemove(userId, out _);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // ---------------------------------------------------------
    // 3. متد اصلی: آپدیت لوکیشن راننده (مثل اوبر)
    // ---------------------------------------------------------
    public async Task UpdateDriverLocation(double lat, double lng, int? tripId)
    {
        // الف) اگر راننده در سفر باشه (مسافر داره)
        if (tripId.HasValue)
        {
            // فقط به گروهِ اون سفر خاص خبر بده (راننده + مسافر)
            // این باعث میشه ترافیک بیخودی برای بقیه ایجاد نشه
            await Clients.Group($"Trip_{tripId}").SendAsync("ReceiveLocationUpdate", lat, lng);
        }
        else
        {
            // ب) اگر راننده آزاده (منتظر مسافره)
            // فعلاً برای تست به همه خبر میدیم (بعداً این رو برمیداریم و فقط تو Redis میریزیم)
            // این خط فقط برای اینه که الان بتونیم روی نقشه حرکت رو ببینیم
            await Clients.All.SendAsync("ReceiveDriverLocation", new { DriverId = Context.UserIdentifier, Lat = lat, Lng = lng });
        }
    }

    // ---------------------------------------------------------
    // 4. متد شروع سفر: ساختن گروه خصوصی
    // ---------------------------------------------------------
    public async Task JoinTripGroup(int tripId)
    {
        // راننده و مسافر این متد رو صدا میزنن تا وارد "اتاق گفتگوی سفر" بشن
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Trip_{tripId}");
    }
}