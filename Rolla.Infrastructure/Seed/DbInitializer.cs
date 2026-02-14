using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Rolla.Domain.Entities;

namespace Rolla.Infrastructure.Seed;

public static class DbInitializer
{
    public static async Task SeedRolesAndAdminAsync(RoleManager<IdentityRole> roleManager)
    {
        // ساخت نقش‌ها اگر وجود نداشته باشند
        string[] roleNames = { "Admin", "Driver", "Rider" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }
}