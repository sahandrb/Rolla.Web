using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Rolla.Application.Interfaces;
using Rolla.Domain.Entities;

namespace Rolla.Application.Services
{
    public class WalletService : IWalletService
    {
        private readonly IApplicationDbContext _context;
        // چون تراکنش دیتابیس داریم، باید به DbContext واقعی کست کنیم تا به .Database دسترسی داشته باشیم
        // اما در معماری تمیز معمولاً یک IUnitOfWork می‌سازند. اینجا برای سادگی مستقیم عمل می‌کنیم.

        public WalletService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task ProcessTripPaymentAsync(int tripId, string riderId, string driverId, decimal amount)
        {
            // محاسبه سهم ها (مثلاً ۲۰ درصد کمیسیون)
            var commissionRate = 0.20m;
            var commissionAmount = amount * commissionRate;
            var driverEarnings = amount - commissionAmount;

            // دسترسی به مسافر و راننده
            var rider = await _context.Users.FindAsync(riderId);
            var driver = await _context.Users.FindAsync(driverId);

            if (rider == null || driver == null) throw new Exception("User not found");

            // 1. کسر از مسافر
            rider.WalletBalance -= amount;
            _context.WalletTransactions.Add(new WalletTransaction
            {
                UserId = riderId,
                Amount = -amount,
                Type = TransactionType.TripPayment,
                RelatedTripId = tripId,
                Description = $"پرداخت بابت سفر {tripId}"
            });

            // 2. واریز به راننده (سهم خالص)
            driver.WalletBalance += driverEarnings;
            _context.WalletTransactions.Add(new WalletTransaction
            {
                UserId = driverId,
                Amount = driverEarnings,
                Type = TransactionType.TripIncome,
                RelatedTripId = tripId,
                Description = $"درآمد سفر {tripId} (پس از کسر کمیسیون)"
            });

            // 3. ثبت درآمد شرکت (اختیاری - فعلا فقط لاگ می‌کنیم)
            // CompanyWallet += commissionAmount;

            await _context.SaveChangesAsync();
        }

        public async Task<decimal> GetBalanceAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.WalletBalance ?? 0;
        }
        // ... (متدهای قبلی سر جایشان بمانند) ...

        // ۱. متد شارژ دستی (شبیه‌ساز درگاه بانک)
        public async Task ChargeWalletAsync(string userId, decimal amount, string description)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new Exception("User not found");

            // افزایش موجودی
            user.WalletBalance += amount;

            // ثبت تراکنش
            _context.WalletTransactions.Add(new WalletTransaction
            {
                UserId = userId,
                Amount = amount,
                Type = TransactionType.Deposit,
                Description = description,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        // ۲. متد دریافت تاریخچه (جدیدترین‌ها اول)
        public async Task<List<WalletTransaction>> GetUserTransactionsAsync(string userId)
        {
            return await _context.WalletTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt) // نزولی بر اساس زمان
                .ToListAsync();
        }
    }
}