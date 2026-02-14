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
}

