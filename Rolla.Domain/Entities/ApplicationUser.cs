using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Rolla.Domain.Entities
{

    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; } = default!;
        // فیلدهای اختصاصی بیزنس شما
        public bool IsDriver { get; set; }
        public string? CarModel { get; set; }  // فقط برای راننده
        public string? PlateNumber { get; set; } // پلاک ماشین
    }
}
