using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rolla.Application.DTOs.Trip;
using Rolla.Application.Interfaces;
using System.Security.Claims;

namespace Rolla.Web.Controllers;

[Authorize] // فقط کاربران لاگین شده
[ApiController]
[Route("api/[controller]")]
public class TripApiController : ControllerBase
{
    private readonly ITripService _tripService;

    public TripApiController(ITripService tripService)
    {
        _tripService = tripService;
    }

    [HttpPost("request")]
    public async Task<IActionResult> RequestTrip([FromBody] CreateTripDto dto)
    {
        // پیدا کردن آیدی کاربری که لاگین کرده است
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null) return Unauthorized();

        var tripId = await _tripService.CreateTripAsync(dto, userId);

        return Ok(new { Message = "سفر با موفقیت ثبت شد و در انتظار راننده است", TripId = tripId });
    }
    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearby([FromQuery] double lat, [FromQuery] double lng)
    {
        // استفاده از سرویسی که برای ردیس نوشتیم
        var geoService = HttpContext.RequestServices.GetRequiredService<IGeoLocationService>();

        // جستجوی راننده‌ها در شعاع ۱۰ کیلومتری
        var drivers = await geoService.GetNearbyDriversAsync(lat, lng, 10);

        return Ok(new { Count = drivers.Count, DriverIds = drivers });
    }
}

