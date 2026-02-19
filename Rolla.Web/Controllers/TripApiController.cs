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
        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (driverId == null) return Unauthorized();

        // ✅ فقط یک خط کد! همه کارها در سرویس انجام می‌شود.
        var trip = await _tripService.AcceptTripAsync(tripId, driverId);

        if (trip == null) return BadRequest("سفر توسط راننده دیگری رزرو شد.");

        return Ok(new { Message = "سفر رزرو شد", RiderId = trip.RiderId });
    }




    [HttpPost("arrive/{tripId}")]
    public async Task<IActionResult> ArriveAtOrigin(int tripId)
    {
        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (driverId == null) return Unauthorized();

        // ✅ استفاده از متد اختصاصی (بدون لاجیک اضافه در کنترلر)
        var success = await _tripService.ArriveAtOriginAsync(tripId, driverId);

        if (!success) return BadRequest("امکان تغییر وضعیت وجود ندارد (شاید سفر لغو شده است).");

        return Ok(new { Message = "وضعیت: رسیدم به مبدا" });
    }

    [HttpPost("start/{tripId}")]
    public async Task<IActionResult> StartTrip(int tripId)
    {
        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (driverId == null) return Unauthorized();

        // ✅ استفاده از متد اختصاصی
        var success = await _tripService.StartTripAsync(tripId, driverId);

        if (!success) return BadRequest("امکان شروع سفر وجود ندارد.");

        return Ok(new { Message = "سفر شروع شد" });
    }

    [HttpPost("finish/{tripId}")]
    public async Task<IActionResult> FinishTrip(int tripId)
    {
        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (driverId == null) return Unauthorized();

        // ✅ لاجیک اتمیک در سرویس
        var success = await _tripService.FinishTripAsync(tripId, driverId);

        if (!success) return BadRequest("خطا در پایان سفر.");

        return Ok(new { Message = "سفر پایان یافت و هزینه دریافت شد." });
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



    // فقط تزریق سرویس و یک اکشن ساده برای تاریخچه
    [HttpGet("chat-history/{tripId}")]
    public async Task<IActionResult> GetChatHistory(int tripId, [FromServices] IChatService chatService)
    {
        // کنترلر هیچ منطقی ندارد، فقط خروجی سرویس را برمی‌گرداند
        var history = await chatService.GetChatHistoryAsync(tripId);
        return Ok(history);
    }


    [HttpGet("navigation/{tripId}")]
    public async Task<IActionResult> GetNavigation(int tripId)
    {
        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (driverId == null) return Unauthorized();

        var route = await _tripService.GetNavigationRouteAsync(tripId, driverId);

        if (route == null) return NotFound("اطلاعات مسیریابی در دسترس نیست.");

        return Ok(route);
    }
}
