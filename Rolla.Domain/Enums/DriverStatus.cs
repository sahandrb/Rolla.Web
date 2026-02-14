using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rolla.Domain.Enums
{
    public enum DriverStatus
    {
        None = 0,       // کاربر عادی است
        Pending = 1,    // ثبت‌نام کرده، منتظر تایید ادمین
        Approved = 2,   // تایید شده، می‌تواند مسافر بگیرد
        Rejected = 3,   // رد شده (مثلاً مدارک ناقص)
        Blocked = 4     // مسدود شده (تخلف)
    }
}
