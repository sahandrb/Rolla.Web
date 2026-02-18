using Microsoft.AspNetCore.SignalR;
using Rolla.Application.Interfaces;
namespace Rolla.Web.Hubs;

public class RideHub : Hub
{
    private readonly ITrackingService _trackingService; // ✅ فقط سرویس لاجیک
    private readonly IChatService _chatService; // ✅ اضافه شد

    public RideHub(ITrackingService trackingService, IChatService chatService)
    {
        _trackingService = trackingService;
        _chatService = chatService;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }
        await base.OnConnectedAsync();
    }

    // ✅ متد تمیز شده: فقط دریافت و پاس دادن به سرویس
    public async Task UpdateDriverLocation(double lat, double lng, int? tripId)
    {
        var driverId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(driverId)) return;

        // هیچ تصمیمی اینجا گرفته نمی‌شود! فقط داده پاس داده می‌شود.
        await _trackingService.ProcessDriverLocationAsync(driverId, lat, lng, tripId);
    }

    // مدیریت گروه‌ها (این کار فنی SignalR است و می‌تواند اینجا بماند)
    public async Task JoinTripGroup(int tripId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Trip_{tripId}");
    }

    public async Task LeaveTripGroup(int tripId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Trip_{tripId}");
    }

    // اضافه کردن متد جدید به کلاس RideHub
    public async Task SendChatMessage(int tripId, string message)
    {
        var senderId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(senderId)) return;

        await _chatService.SaveMessageAsync(tripId, senderId, message);
        // ۱. ذخیره در دیتابیس (از طریق لایه Application)

        // ۲. پخش زنده پیام برای طرف مقابل در همان گروه سفر
        await Clients.Group($"Trip_{tripId}").SendAsync("ReceiveChatMessage", senderId, message);
    }



}