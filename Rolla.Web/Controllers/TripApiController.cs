using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rolla.Application.DTOs.Trip;
using Rolla.Application.Interfaces;
using System.Security.Claims;

namespace Rolla.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TripApiController : ControllerBase
{
    private readonly ITripService _tripService;
    private readonly IGeoLocationService _geoService; // ۱. فیلد جدید

    // ۲. تزریق وابستگی در سازنده
    public TripApiController(ITripService tripService, IGeoLocationService geoService)
    {
        _tripService = tripService;
        _geoService = geoService;
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
}