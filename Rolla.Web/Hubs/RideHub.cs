using Microsoft.AspNetCore.SignalR;
using Rolla.Application.Interfaces;
using Rolla.Web.Services; // اضافه کردن این یوزینگ برای دسترسی به Aggregator
using System.Collections.Concurrent;

namespace Rolla.Web.Hubs;

public class RideHub : Hub
{
    private readonly LocationAggregator _aggregator;

    // ۱. تزریق بافر هوشمند به هاب
    public RideHub(LocationAggregator aggregator)
    {
        _aggregator = aggregator;
    }

    // لیست آنلاین‌ها (فعلاً برای تست، در آینده به ردیس منتقل می‌شود)
    private static readonly ConcurrentDictionary<string, string> _onlineUsers = new();

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            _onlineUsers[userId] = Context.ConnectionId;
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }
        await base.OnConnectedAsync();
    }

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
    // ۳. متد اصلی: آپدیت لوکیشن با استراتژی Aggregation
    // ---------------------------------------------------------
    public async Task UpdateDriverLocation(double lat, double lng, int? tripId)
    {
        var driverId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(driverId)) return;

        // گام اول: دیتا رو همیشه به بافر بده تا هر ۲ ثانیه دسته‌جمعی برن تو ردیس
        // این همون Aggregation هست که بار رو از رو دیتابیس و شبکه برمیداره
        _aggregator.AddLocation(driverId, lat, lng);

        // گام دوم: مدیریت ارسال زنده (Real-time)
        if (tripId.HasValue)
        {
            // الف) اگر راننده در سفر است: 
            // بلافاصله مختصات رو به مسافرش بفرست (Fast Track)
            // مسافر نباید ۲ ثانیه صبر کنه، اون باید حرکت رو نرم ببینه
            await Clients.Group($"Trip_{tripId}").SendAsync("ReceiveLocationUpdate", lat, lng);
        }
        else
        {
            // ب) اگر راننده آزاد است:
            // اینجا دیگه Clients.All.SendAsync نمی‌زنیم! 🛑
            // چون مسافرها راننده‌های نزدیک رو از طریق API و از "ردیس" میگیرن.
            // این کار باعث میشه مصرف پهنای باند سیستم به شدت کم بشه.

            // فقط برای اینکه الان تو کنسول تست ببینی لوکیشن دریافت شده:
            // await Clients.Caller.SendAsync("Log", "لوکیشن شما در بافر ذخیره شد");
        }
    }

    public async Task JoinTripGroup(int tripId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Trip_{tripId}");
    }
}