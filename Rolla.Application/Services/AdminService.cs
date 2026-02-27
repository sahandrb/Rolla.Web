using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rolla.Application.Interfaces;
using Rolla.Domain.Entities;
using Rolla.Domain.Common.Constants;

namespace Rolla.Application.Services;

public class AdminService : IAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<bool> PromoteToAdminAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return false;

        // اگر الان سوپر ادمین است، نمی‌شود تغییرش داد (امنیت)
        if (await _userManager.IsInRoleAsync(user, Roles.SuperAdmin)) return false;

        if (!await _userManager.IsInRoleAsync(user, Roles.Admin))
        {
            await _userManager.AddToRoleAsync(user, Roles.Admin);
            return true;
        }
        return false;
    }

    public async Task<List<string>> GetAllAdminsAsync()
    {
        // لیست ایمیل کسانی که ادمین هستند
        var admins = await _userManager.GetUsersInRoleAsync(Roles.Admin);
        return admins.Select(u => u.Email!).ToList();
    }  

    public async Task<bool> RevokeAdminAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return false;

        // نمی‌توان سوپر ادمین را عزل کرد
        if (await _userManager.IsInRoleAsync(user, Roles.SuperAdmin)) return false;

        await _userManager.RemoveFromRoleAsync(user, Roles.Admin);
        return true;
    }
}