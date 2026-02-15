using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Rolla.Domain.Entities;
using Rolla.Domain.Enums;

namespace Rolla.Web.Areas.Driver.Controllers
{
    [Area("Driver")]
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            // سناریوی چک کردن وضعیت
            if (user.DriverStatus == DriverStatus.Pending)
            {
                return View("Pending"); // صفحه‌ای که می‌گوید "در حال بررسی..."
            }

            if (user.DriverStatus != DriverStatus.Approved)
            {
                return RedirectToAction("Register"); // اگر هنوز ثبت نام نکرده
            }

            return View(); // فایل WorkBoard.cshtml
        }
    }
}