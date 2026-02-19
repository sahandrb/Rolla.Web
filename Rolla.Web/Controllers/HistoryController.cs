using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Rolla.Application.Interfaces;
using System.Security.Claims;

namespace Rolla.Web.Controllers;

[Authorize]
public class HistoryController : Controller
{
    private readonly ITripService _tripService;

    public HistoryController(ITripService tripService)
    {
        _tripService = tripService;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        // 🔒 استخراج آیدی کاربر از توکن/کوکی (بسیار امن)
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        const int pageSize = 10;
        var history = await _tripService.GetTripHistoryAsync(userId, page, pageSize);

        return View(history);
    }
}