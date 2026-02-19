using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rolla.Application.DTOs.Auth;
using Rolla.Application.Interfaces;
using Rolla.Domain.Enums;
using Rolla.Domain.Exceptions;
using System.Security.Claims;

namespace Rolla.Web.Areas.Driver.Controllers;

[Area("Driver")]
[Authorize]
public class RegisterController : Controller
{
    private readonly IDriverService _driverService;

    public RegisterController(IDriverService driverService)
    {
        _driverService = driverService;
    }

    // GET: نمایش فرم
    [HttpGet]
    public IActionResult Index()
    {
        // اگر کاربر قبلاً درخواست داده، به داشبورد برود
        // (این منطق می‌تواند پیچیده‌تر باشد ولی فعلا ساده نگه می‌داریم)
        return View();
    }

    // POST: پردازش فرم
    [HttpPost]
    [ValidateAntiForgeryToken] // امنیت Rule #13
    public async Task<IActionResult> Index(RegisterDriverDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // تمام منطق ذخیره‌سازی و فایل در سرویس است
            await _driverService.RegisterDriverAsync(dto, userId!);

            TempData["Success"] = "مدارک شما با موفقیت ارسال شد و در انتظار بررسی است.";
            return RedirectToAction("Index", "Dashboard"); // بازگشت به داشبورد
        }
        catch (BusinessRuleException ex)
        {
            // خطاهای منطقی (مثل فرمت فایل اشتباه)
            ModelState.AddModelError("", ex.Message);
            return View(dto);
        }
        catch (Exception)
        {
            // خطاهای سیستم
            ModelState.AddModelError("", "خطایی در سیستم رخ داده است. لطفاً مجدد تلاش کنید.");
            return View(dto);
        }
    }
}