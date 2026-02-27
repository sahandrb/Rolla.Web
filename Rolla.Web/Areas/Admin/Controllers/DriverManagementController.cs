using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // برای ToListAsync
using Rolla.Application.Interfaces;
using Rolla.Domain.Common.Constants;
using Rolla.Domain.Entities;
using Rolla.Domain.Enums;

namespace Rolla.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Admin)]
    public class DriverManagementController : Controller
    {
        private readonly IDriverService _driverService; 

        public DriverManagementController(IDriverService driverService)
        {
            _driverService = driverService;
        }

        public async Task<IActionResult> Index()
        {
            var drivers = await _driverService.GetPendingDriversAsync();
            return View(drivers);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(string userId)
        {
            await _driverService.ApproveDriverAsync(userId);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Reject(string userId)
        {
            await _driverService.RejectDriverAsync(userId);
            return RedirectToAction("Index");
        }
        // 1. نمایش صفحه جزئیات راننده
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var details = await _driverService.GetDriverDetailsAsync(id);
            if (details == null) return NotFound("راننده یافت نشد.");

            return View(details); // ارسال DTO به View
        }

        // 2. اکشن بازگرداندن عکس (Gateway امنیتی فایل‌ها)
        [HttpGet]
        public async Task<IActionResult> GetDocumentImage(int id)
        {
            var result = await _driverService.GetDocumentFileAsync(id);
            if (result == null) return NotFound();

            // تبدیل بایت‌ها به فایل تصویری در مرورگر
            return File(result.Value.FileBytes, result.Value.ContentType);
        }
    }
}