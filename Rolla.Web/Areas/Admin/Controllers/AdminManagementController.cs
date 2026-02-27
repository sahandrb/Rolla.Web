using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rolla.Application.Interfaces;
using Rolla.Domain.Common.Constants;

namespace Rolla.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.SuperAdmin)] //  فقط تو دسترسی داری!
public class AdminManagementController : Controller
{
    private readonly IAdminService _adminService;

    public AdminManagementController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    public async Task<IActionResult> Index()
    {
        var admins = await _adminService.GetAllAdminsAsync();
        return View(admins);
    }

    [HttpPost]
    public async Task<IActionResult> MakeAdmin(string email)
    {
        var result = await _adminService.PromoteToAdminAsync(email);
        if (result) TempData["Success"] = "ادمین جدید اضافه شد.";
        else TempData["Error"] = "کاربر یافت نشد یا عملیات نامعتبر است.";

        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Revoke(string email)
    {
        await _adminService.RevokeAdminAsync(email);
        return RedirectToAction("Index");
    }
}