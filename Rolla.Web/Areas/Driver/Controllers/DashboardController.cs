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
                return RedirectToAction("Register");
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

        // ۲. نمایش فرم ثبت‌نام
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // ۳. پردازش ثبت‌نام
        [HttpPost]
        public async Task<IActionResult> Register(string carModel, string plateNumber)
        {
            var user = await _userManager.GetUserAsync(User);

            user.CarModel = carModel;
            user.PlateNumber = plateNumber;
            user.IsDriver = true;
            user.DriverStatus = DriverStatus.Pending; // مهم: وضعیت میره روی "در حال بررسی"

            await _userManager.UpdateAsync(user);

            return RedirectToAction("Index");
        }
    }
}