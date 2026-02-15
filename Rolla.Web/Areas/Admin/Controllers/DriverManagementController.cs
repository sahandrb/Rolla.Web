using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // برای ToListAsync
using Rolla.Domain.Entities;
using Rolla.Domain.Enums;

namespace Rolla.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    // [Authorize(Roles = "Admin")] // فعلاً کامنت کن تا راحت تست کنی، بعداً فعالش کن
    public class DriverManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public DriverManagementController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // لیست رانندگانی که وضعیتشان None نیست (یعنی درخواست داده‌اند)
            var drivers = await _userManager.Users
                .Where(u => u.IsDriver || u.DriverStatus != DriverStatus.None)
                .ToListAsync();

            return View(drivers);
        }
        [HttpPost]
        public async Task<IActionResult> Approve([FromForm] string userId) // ۱. اضافه کردن FromForm
        {
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Index");

            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.DriverStatus = DriverStatus.Approved;

                // ۲. اجبار به آپدیت و ذخیره تغییرات
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    // اگر خطایی رخ داد (مثلاً مشکل دیتابیس)، اینجا لاگ کن
                    // فعلاً برای سادگی چیزی برنمی‌گردانیم
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Reject([FromForm] string userId)
        {
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Index");

            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.DriverStatus = DriverStatus.Rejected;
                await _userManager.UpdateAsync(user);
            }
            return RedirectToAction("Index");
        }
    }
}