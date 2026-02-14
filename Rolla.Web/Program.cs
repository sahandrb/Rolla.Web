using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rolla.Application.Interfaces;
using Rolla.Application.Services;
using Rolla.Domain.Entities;
using Rolla.Infrastructure.Data;
using Rolla.Infrastructure.Services; // اضافه شد
using Rolla.Web.Hubs;
using Rolla.Web.Services;
using StackExchange.Redis; // اضافه شد

var builder = WebApplication.CreateBuilder(args);

// 1. تنظیم دیتابیس
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, x =>
        x.UseNetTopologySuite()
    ));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// 2. تنظیم Identity
builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

// 3. ثبت سرویس‌های پایه
builder.Services.AddScoped<IApplicationDbContext>(provider =>
    provider.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<ITripService, TripService>();

// 4. ثبت SignalR و نوتیفیکیشن
builder.Services.AddSignalR();
builder.Services.AddScoped<INotificationService, NotificationService>();

// ---------------------------------------------------------
// 🚨 بخش گمشده کد شما (اضافه کردن ردیس)
// ---------------------------------------------------------
// الف) ایجاد اتصال به Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect("localhost:6379"));

// ب) معرفی سرویس لوکیشن به دات‌نت (حل مشکل خطای InvalidOperationException)
builder.Services.AddScoped<IGeoLocationService, RedisLocationService>();
// ---------------------------------------------------------

// 5. ثبت بافر و کارگر پس‌زمینه (Aggregation)
builder.Services.AddSingleton<LocationAggregator>();
builder.Services.AddHostedService<LocationUploadService>();

var app = builder.Build();

// 6. تنظیمات Pipeline
app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapHub<RideHub>("/rideHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();