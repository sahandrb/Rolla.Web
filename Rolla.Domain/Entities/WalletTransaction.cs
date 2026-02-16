using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rolla.Domain.Common;
using Rolla.Domain.Enums;

namespace Rolla.Domain.Entities
{
    public class WalletTransaction : BaseEntity
    {
        public string UserId { get; set; } = default!; // صاحب کیف پول
        public decimal Amount { get; set; } // مبلغ (مثبت: واریز، منفی: برداشت)
        public TransactionType Type { get; set; } // بابت چی بوده؟
        public int? RelatedTripId { get; set; } // مرتبط با کدوم سفر؟
        public string Description { get; set; } = default!;
    }

    public enum TransactionType
    {
        Deposit = 1,      // شارژ حساب
        TripPayment = 2,  // پرداخت هزینه سفر (برای مسافر)
        TripIncome = 3,   // درآمد سفر (برای راننده)
        Commission = 4    // کسر کمیسیون
    }
}