using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rolla.Application.DTOs.Trip;
using Rolla.Application.Interfaces;
using Rolla.Application.Services;
using Rolla.Domain.Enums;
using System.Security.Claims;

namespace Rolla.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TripApiController : ControllerBase
{
    private readonly ITripService _tripService;
    private readonly IGeoLocationService _geoService; // ۱. فیلد جدید
    private readonly IWalletService _walletService;

    // ۲. تزریق وابستگی در سازنده
    public TripApiController(ITripService tripService, IGeoLocationService geoService , IWalletService walletService)
    {
        _tripService = tripService;
        _geoService = geoService;
        _walletService = walletService;
    }

    [HttpPost("request")]
    public async Task<IActionResult> RequestTrip([FromBody] CreateTripDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var tripId = await _tripService.CreateTripAsync(dto, userId);

        return Ok(new { Message = "سفر ثبت شد", TripId = tripId });
    }

    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearby([FromQuery] double lat, [FromQuery] double lng)
    {
        // ۳. استفاده از فیلد تزریق شده (حذف HttpContext.RequestServices)
        var drivers = await _geoService.GetNearbyDriversAsync(lat, lng, 10);

        return Ok(new { Count = drivers.Count, DriverIds = drivers });
    }
    [HttpGet("seed-drivers")]
    public async Task<IActionResult> SeedDrivers()
    {
        // این کد دستی ۳ تا راننده خیالی اطراف تو در ردیس می‌کارد
        await _geoService.UpdateDriverLocationAsync("Driver_Ali", 35.71, 51.41);
        await _geoService.UpdateDriverLocationAsync("Driver_Reza", 35.715, 51.415);
        await _geoService.UpdateDriverLocationAsync("Driver_Sara", 35.72, 51.42);

        return Ok("۳ راننده فرضی در دیتابیس کاشته شدند!");
    }
    // اضافه کردن این متد به TripApiController
    [HttpGet("calculate")]
    public IActionResult CalculatePrice(double oLat, double oLng, double dLat, double dLng)
    {
        // اینجا از سرویس PricingService استفاده کن که در مرحله ۱ ساختی
        // برای سادگی فعلا دستی اینجکت نکن، مستقیم New کن یا بهتره اینجکت کنی
        var pricingService = HttpContext.RequestServices.GetRequiredService<IPricingService>();
        var price = pricingService.CalculatePrice(oLat, oLng, dLat, dLng);
        return Ok(new { Price = price });
    }
    [HttpPost("accept/{tripId}")]
    public async Task<IActionResult> AcceptTrip(int tripId)
    {
        // دریافت آیدی راننده از توکن
        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // حل خطای Null Reference: اگر راننده لاگین نبود یا آیدی نداشت
        if (string.IsNullOrEmpty(driverId))
        {
            return Unauthorized("شما باید لاگین باشید.");
        }

        // سرویس صدا زده می‌شود و نتیجه (سفر آپدیت شده) برمی‌گردد
        var acceptedTrip = await _tripService.AcceptTripAsync(tripId, driverId);

        if (acceptedTrip != null)
        {
            // حالا که سفر تایید شده، RiderId را داریم و می‌توانیم نوتیفیکیشن بفرستیم
            // نکته: INotificationService باید در سازنده تزریق شده باشد، یا اینجا دستی بگیریم
            var notifService = HttpContext.RequestServices.GetRequiredService<INotificationService>();

            await notifService.NotifyTripAcceptedAsync(tripId, acceptedTrip.RiderId, driverId);

            return Ok(new { Message = "سفر به شما اختصاص یافت", RiderId = acceptedTrip.RiderId });
        }

        return BadRequest("خطا: سفر یافت نشد یا توسط راننده دیگری رزرو شده است.");
    }
    [HttpPost("arrive/{tripId}")]
    public async Task<IActionResult> ArriveAtOrigin(int tripId)
    {
        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (driverId == null) return Unauthorized();

        var riderId = await _tripService.ChangeTripStatusAsync(tripId, driverId, TripStatus.Arrived);
        if (riderId == null) return BadRequest("سفر یافت نشد.");

        var notif = HttpContext.RequestServices.GetRequiredService<INotificationService>();
        // ارسال پیام خاص برای آپدیت UI مسافر
        await notif.NotifyStatusChangeAsync(riderId, "Arrived");

        return Ok(new { Message = "وضعیت: رسیدم به مبدا" });
    }

    [HttpPost("start/{tripId}")]
    public async Task<IActionResult> StartTrip(int tripId)
    {
        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (driverId == null) return Unauthorized();

        var riderId = await _tripService.ChangeTripStatusAsync(tripId, driverId, TripStatus.Started);
        if (riderId == null) return BadRequest();

        var notif = HttpContext.RequestServices.GetRequiredService<INotificationService>();
        await notif.NotifyStatusChangeAsync(riderId, "Started");

        return Ok(new { Message = "سفر شروع شد" });
    }

    [HttpPost("finish/{tripId}")]
    public async Task<IActionResult> FinishTrip(int tripId)
    {
        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (driverId == null) return Unauthorized();

        // 1. تغییر وضعیت سفر به پایان یافته
        var riderId = await _tripService.ChangeTripStatusAsync(tripId, driverId, TripStatus.Finished);
        if (riderId == null) return BadRequest();

        // 2. دریافت اطلاعات سفر برای محاسبه قیمت (در واقعیت باید قیمت نهایی اینجا محاسبه شود)
        // اینجا فرض می‌کنیم قیمت همان قیمت اولیه است. برای سادگی یک کوئری ساده می‌زنیم
        var dbContext = HttpContext.RequestServices.GetRequiredService<IApplicationDbContext>();
        var trip = await dbContext.Trips.FindAsync(tripId);

        if (trip != null)
        {
            try
            {
                // 3. انجام عملیات مالی (کسر از مسافر، واریز به راننده)
                await _walletService.ProcessTripPaymentAsync(tripId, riderId, driverId, trip.Price);
            }
            catch (Exception ex)
            {
                // اگر موجودی کافی نبود یا خطا خورد
                return BadRequest(new { Message = "خطا در پرداخت: " + ex.Message });
            }
        }

        // 4. خبر دادن به مسافر
        var notif = HttpContext.RequestServices.GetRequiredService<INotificationService>();
        await notif.NotifyStatusChangeAsync(riderId, "Finished");

        return Ok(new { Message = "سفر پایان یافت و هزینه پرداخت شد." });
    }
}
