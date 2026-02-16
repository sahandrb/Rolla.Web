using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rolla.Domain.Entities;

namespace Rolla.Application.Interfaces
{
    public interface IWalletService
    {
        // انتقال پول بین مسافر و راننده در پایان سفر
        Task ProcessTripPaymentAsync(int tripId, string riderId, string driverId, decimal amount);

        // دریافت موجودی
        Task<decimal> GetBalanceAsync(string userId);


        // ✨ این دو متد جدید را اضافه کن:
        Task ChargeWalletAsync(string userId, decimal amount, string description);
        Task<List<WalletTransaction>> GetUserTransactionsAsync(string userId);

    }
}
