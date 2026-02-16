using Microsoft.AspNetCore.Identity;
using Rolla.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rolla.Domain.Entities
{

    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public bool IsDriver { get; set; } // این فیلد قدیمی است اما نگهش دار
        public DriverStatus DriverStatus { get; set; } = DriverStatus.None; // ✨ فیلد جدید

        public string? CarModel { get; set; }
        public string? PlateNumber { get; set; }
        public decimal WalletBalance { get; set; } = 0; // موجودی فعلی (Cache شده)
    }
}
