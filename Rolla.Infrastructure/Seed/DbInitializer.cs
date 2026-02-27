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

        string[] roleNames = { Roles.SuperAdmin, Roles.Admin, Roles.Driver, Roles.Rider };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // 2. ساخت اکانت سوپرادمین)
        var myEmail = "sahand@gmail.com";
        var superUser = await userManager.FindByEmailAsync(myEmail);

        if (superUser == null)
        {
            superUser = new ApplicationUser
            {
                UserName = myEmail,
                Email = myEmail,
                EmailConfirmed = true,
                FullName = "Sahand (Owner)",
                WalletBalance = 999999999 // پول بی‌نهایت برای تست :))
            };

            var result = await userManager.CreateAsync(superUser, "Sahand123!"); // 👈 رمز عبور قوی بگذار
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(superUser, Roles.SuperAdmin);
            }
        }
    }
}