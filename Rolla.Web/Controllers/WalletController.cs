using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rolla.Application.Interfaces;
using System.Security.Claims;

namespace Rolla.Web.Controllers
{
    [Authorize] // فقط لاگین شده‌ها
    public class WalletController : Controller
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        // نمایش صفحه کیف پول
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // گرفتن موجودی و تاریخچه
            var balance = await _walletService.GetBalanceAsync(userId);
            var transactions = await _walletService.GetUserTransactionsAsync(userId);

            ViewBag.Balance = balance;
            return View(transactions);
        }

        // اکشن شارژ حساب (POST)
        [HttpPost]
        public async Task<IActionResult> Charge(decimal amount)
        {
            if (amount < 1000)
            {
                TempData["Error"] = "مبلغ شارژ باید حداقل ۱۰۰۰ تومان باشد.";
                return RedirectToAction("Index");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // فراخوانی سرویس برای شارژ
            await _walletService.ChargeWalletAsync(userId, amount, "شارژ آنلاین حساب (تستی)");

            TempData["Success"] = "حساب شما با موفقیت شارژ شد ✅";
            return RedirectToAction("Index");
        }
    }
}