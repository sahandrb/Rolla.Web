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

        // ۱. صفحه اصلی (میز کار)
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Redirect("/Identity/Account/Login");

            // اگر هنوز ثبت‌نام نکرده -> برو ثبت‌نام
            if (user.DriverStatus == DriverStatus.None)
            {
                return RedirectToAction("Index", "Register", new { area = "Driver" });
            }

            // اگر منتظر تایید است -> برو صفحه انتظار
            if (user.DriverStatus == DriverStatus.Pending)
            {
                return View("Pending");
            }

            // اگر رد شده -> برو صفحه رد
            if (user.DriverStatus == DriverStatus.Rejected)
            {
                return View("Rejected");
            }

            // اگر تایید شده -> برو داشبورد اصلی
            return View();
        }

    }
}