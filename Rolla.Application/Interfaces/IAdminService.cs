using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Rolla.Application.Interfaces;
public interface IAdminService
{
    // تبدیل کاربر عادی به ادمین
    Task<bool> PromoteToAdminAsync(string email);

    // گرفتن لیست تمام ادمین‌ها (برای نمایش به تو)
    Task<List<string>> GetAllAdminsAsync();

    // عزل ادمین (اختیاری)
    Task<bool> RevokeAdminAsync(string email);
}