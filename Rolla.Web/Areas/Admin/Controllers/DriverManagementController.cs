using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // برای ToListAsync
using Rolla.Application.Interfaces;
using Rolla.Domain.Entities;
using Rolla.Domain.Enums;

namespace Rolla.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // بعداً فعالش کن
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
    }
}