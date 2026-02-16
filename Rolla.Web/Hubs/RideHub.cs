using Microsoft.AspNetCore.SignalR;
using Rolla.Application.Interfaces;
using Rolla.Web.Services;
using System.Collections.Concurrent;

namespace Rolla.Web.Hubs;

public class RideHub : Hub
{
    private readonly LocationAggregator _aggregator;

    public RideHub(LocationAggregator aggregator)
    {
        _aggregator = aggregator;
    }

    // ۱. وقتی کاربر وصل می‌شود (چه راننده چه مسافر)
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            // عضویت در گروه اختصاصی خود کاربر برای دریافت نوتیفیکیشن‌های شخصی (مثل پیشنهاد سفر)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }
        await base.OnConnectedAsync();
    }

    // ۲. آپدیت لوکیشن راننده
    public async Task UpdateDriverLocation(double lat, double lng, int? tripId)
    {
        var driverId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(driverId)) return;

        // ذخیره در بافر برای انتقال به ردیس (هر ۲ ثانیه یکبار)
        _aggregator.AddLocation(driverId, lat, lng);

        // اگر راننده در حال انجام سفر است، لوکیشن را "زنده" برای مسافر بفرست
        if (tripId.HasValue)
        {
            // نام متد را ReceiveDriverLocation می‌گذاریم تا در سمت مسافر گوش دهیم
            await Clients.Group($"Trip_{tripId.Value}").SendAsync("ReceiveDriverLocation", lat, lng);
        }
    }

    // ۳. ورود به گروه سفر (هم راننده و هم مسافر باید این را صدا بزنند)
    public async Task JoinTripGroup(int tripId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Trip_{tripId}");
    }

    // ۴. خروج از گروه سفر (وقتی سفر تمام یا لغو شد)
    public async Task LeaveTripGroup(int tripId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Trip_{tripId}");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // سیگنال آر خودش کاربر را از گروه‌ها خارج می‌کند، نیاز به کد اضافه نیست
        await base.OnDisconnectedAsync(exception);
    }
}