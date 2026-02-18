using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rolla.Application.Interfaces;
using Rolla.Domain.Entities;
using Rolla.Domain.Enums;

namespace Rolla.Application.Services;

public class DriverService : IDriverService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public DriverService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<List<ApplicationUser>> GetPendingDriversAsync()
    {
        return await _userManager.Users
            .Where(u => u.DriverStatus == DriverStatus.Pending)
            .ToListAsync();
    }

    public async Task<bool> ApproveDriverAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        user.DriverStatus = DriverStatus.Approved;
        user.IsDriver = true;

        await _userManager.UpdateAsync(user);

        // اضافه کردن نقش امنیتی
        if (!await _userManager.IsInRoleAsync(user, "Driver"))
        {
            await _userManager.AddToRoleAsync(user, "Driver");
        }
        return true;
    }

    public async Task<bool> RejectDriverAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        user.DriverStatus = DriverStatus.Rejected;
        await _userManager.UpdateAsync(user);
        return true;
    }
}