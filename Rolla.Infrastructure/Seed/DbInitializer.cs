using Microsoft.AspNetCore.Identity;
using Rolla.Domain.Entities;
using Rolla.Domain.Common.Constants;

namespace Rolla.Infrastructure.Seed;

public static class DbInitializer
{
    public static async Task SeedRolesAndSuperAdminAsync(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager)
    {
        // ۱. ساخت نقش‌ها
        string[] roleNames = { Roles.SuperAdmin, Roles.Admin, Roles.Driver, Roles.Rider };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
                await roleManager.CreateAsync(new IdentityRole(roleName));
        }

        // ۲. ساخت اکانت شما
        var myEmail = "sahandT@gmail.com"; // 👈 ایمیل دقیق شما
        var superUser = await userManager.FindByEmailAsync(myEmail);

        if (superUser == null)
        {
            superUser = new ApplicationUser
            {
                UserName = myEmail,
                Email = myEmail,
                EmailConfirmed = true,
                FullName = "Sahand (Founder)",
                WalletBalance = 1000000
            };

            // رمز عبور: Sahand123!
            var result = await userManager.CreateAsync(superUser, "Sahand123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(superUser, Roles.SuperAdmin);
            }
        }
    }
}