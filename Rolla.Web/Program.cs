using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rolla.Application.Interfaces;
using Rolla.Application.Services;
using Rolla.Domain.Entities;
using Rolla.Infrastructure.Data;
using Rolla.Infrastructure.Services; // اضافه شده برای RedisLocationService
using Rolla.Web.Hubs;
using Rolla.Web.Services;
using StackExchange.Redis; // اضافه شده برای اتصال به ردیس

var builder = WebApplication.CreateBuilder(args);

// ====================================================
// 1. تنظیمات دیتابیس و زیرساخت (Database & Infrastructure)
// ====================================================

// دریافت کانکشن SQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, x =>
        x.UseNetTopologySuite() // پشتیبانی از داده‌های جغرافیایی
    ));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// دریافت کانکشن Redis (برای ذخیره لوکیشن‌ها)
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect("localhost:6379"));

// ====================================================
// 2. تنظیمات هویت (Identity)
// ====================================================
builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// ====================================================
// 3. سرویس‌های وب (Web Framework Services)
// ====================================================
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // برای صفحات لاگین و رجیستر
builder.Services.AddSignalR();    // برای ارتباط زنده

// ====================================================
// 4. تزریق وابستگی‌ها (Dependency Injection)
// ====================================================
builder.Services.AddScoped<IApplicationDbContext>(provider =>
    provider.GetRequiredService<ApplicationDbContext>());

builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// سرویس مکان‌یابی با ردیس (حل مشکل خطای قبلی شما)
builder.Services.AddScoped<IGeoLocationService, RedisLocationService>();

// ====================================================
// 5. سرویس‌های پس‌زمینه (Background Services)
// ====================================================
// بافر هوشمند (حتماً Singleton)
builder.Services.AddSingleton<LocationAggregator>();

// کارگر تخلیه بافر به دیتابیس/ردیس
builder.Services.AddHostedService<LocationUploadService>();
builder.Services.AddHostedService<TripDispatcherService>(); //  سرویس جدید
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IDriverService, DriverService>();
builder.Services.AddScoped<ITrackingService, TrackingService>();
builder.Services.AddExceptionHandler<Rolla.Web.Infrastructure.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddHttpClient<IRoutingService, OsrmRoutingService>();




var app = builder.Build();
// اضافه کردن کلاینت مسیریابی

// ====================================================
// 6. تنظیم پایپ‌لاین (HTTP Request Pipeline)
// ====================================================

// ✅ کد جدید: همیشه از ExceptionHandler استفاده کن
app.UseExceptionHandler();

// (نکته: در محیط دولوپمنت ممکن است بخواهی صفحه خطای زرد رنگ معروف را ببینی،
// اما برای تست API، دیدن جیسون استاندارد بهتر است. پس فعلاً همین کافیست.)

if (app.Environment.IsDevelopment())
{
    // app.UseMigrationsEndPoint(); // این اگر لازم بود بماند
}
// ...


app.UseHttpsRedirection();
app.UseStaticFiles(); // برای فایل‌های wwwroot
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// مپ کردن استاتیک فایل‌های جدید در دات نت 9
app.MapStaticAssets();

// تنظیم Hub سیگنال آر
app.MapHub<RideHub>("/rideHub");

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();