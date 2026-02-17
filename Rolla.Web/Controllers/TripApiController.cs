using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rolla.Application.DTOs.Trip;
using Rolla.Application.Interfaces;
using Rolla.Domain.Enums;
using System.Security.Claims;

namespace Rolla.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TripApiController : ControllerBase
{
    private readonly ITripService _tripService;
    private readonly IGeoLocationService _geoService;
    private readonly IWalletService _walletService;
    private readonly IApplicationDbContext _context; // ✅ اضافه شده

    // تزریق وابستگی‌ها
    public TripApiController(
        ITripService tripService,
        IGeoLocationService geoService,
        IWalletService walletService,
        IApplicationDbContext context) // ✅ اضافه شده
    {
        _tripService = tripService;
        _geoService = geoService;
        _walletService = walletService;
        _context = context;
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

        // وضعیت در دیتابیس تغییر می‌کند
        var riderId = await _tripService.ChangeTripStatusAsync(tripId, driverId, TripStatus.Arrived);

        // اگر نال بود یعنی مشکلی هست
        if (riderId == null) return BadRequest("سفر یافت نشد.");

        var notif = HttpContext.RequestServices.GetRequiredService<INotificationService>();

        // ✅ اصلاح شد: ارسال tripId به جای riderId
        await notif.NotifyStatusChangeAsync(tripId, "Arrived");

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

        // ✅ اصلاح شد: ارسال tripId
        await notif.NotifyStatusChangeAsync(tripId, "Started");

        return Ok(new { Message = "سفر شروع شد" });
    }

    [HttpPost("finish/{tripId}")]
    public async Task<IActionResult> FinishTrip(int tripId)
    {
        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (driverId == null) return Unauthorized();

        // 1. تغییر وضعیت سفر به پایان یافته
        var riderId = await _tripService.ChangeTripStatusAsync(tripId, driverId, TripStatus.Finished);
        if (riderId == null) return BadRequest(new { Message = "سفر یافت نشد یا شما راننده آن نیستید." });

        // 2. دریافت اطلاعات سفر (قیمت)
        var trip = await _context.Trips.FindAsync(tripId);

        if (trip != null && trip.Price > 0)
        {
            try
            {
                // 3. انجام عملیات مالی
                await _walletService.ProcessTripPaymentAsync(tripId, riderId, driverId, trip.Price);
            }
            catch (Exception ex)
            {
                // خطا را لاگ کنید اما نگذارید پایان سفر متوقف شود
                Console.WriteLine($"Error in payment: {ex.Message}");
                return Ok(new { Message = "سفر تمام شد اما در تراکنش مالی خطایی رخ داد: " + ex.Message });
            }
        }
        else
        {
            // اگر قیمت صفر بود (جهت دیباگ)
            Console.WriteLine("Warning: Trip price is 0 or trip not found.");
        }

        // 4. ارسال نوتیفیکیشن به مسافر (که در مرحله قبل درست کردیم)
        var notif = HttpContext.RequestServices.GetRequiredService<INotificationService>();
        await notif.NotifyStatusChangeAsync(tripId, "Finished");

        return Ok(new { Message = "سفر با موفقیت پایان یافت و تراکنش ثبت شد." });
    }
    [HttpPost("cancel/{tripId}")]
    public async Task<IActionResult> CancelTrip(int tripId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var success = await _tripService.CancelTripAsync(tripId, userId);

        if (!success) return BadRequest("امکان لغو سفر وجود ندارد.");

        return Ok(new { Message = "سفر لغو شد." });
    }


    [HttpPost("reject/{tripId}")]
    public async Task<IActionResult> RejectTrip(int tripId)
    {
        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (driverId == null) return Unauthorized();

        await _tripService.RejectTripAsync(tripId, driverId);

        return Ok(new { Message = "سفر رد شد و دیگر نمایش داده نمی‌شود." });
    }

}
