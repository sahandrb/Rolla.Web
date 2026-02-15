using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rolla.Web.Areas.Rider.Controllers
{
    [Area("Rider")]
    [Authorize] // فقط لاگین شده‌ها
    public class RideController : Controller
    {
        public IActionResult RequestRide()
        {
            return View();
        }
    }
}