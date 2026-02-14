using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rolla.Application.Interfaces;
using Rolla.Application.Services;
using Rolla.Domain.Entities;
using Rolla.Infrastructure.Data;
using Rolla.Web.Hubs;
using Rolla.Web.Services;



var builder = WebApplication.CreateBuilder(args);

// 1. دریافت کانکشن استرینگ
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 2. تنظیم دیتابیس با پشتیبانی از نقشه (NetTopologySuite)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, x =>
        x.UseNetTopologySuite() // <--- این خط برای محاسبات جی‌پی‌اس حیاتی است
    ));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// 3. تنظیم سیستم هویت برای استفاده از کاربر سفارشی ما (ApplicationUser)
builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
    options.SignIn.RequireConfirmedAccount = false; // برای تست فعلاً روی false باشد
    options.Password.RequireDigit = false;          // تنظیمات پسورد (اختیاری)
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;

})
    .AddRoles<IdentityRole>() // اضافه کردن پشتیبانی از نقش‌ها (راننده/مسافر)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

// ثبت اینترفیس دیتابیس
builder.Services.AddScoped<IApplicationDbContext>(provider =>
    provider.GetRequiredService<ApplicationDbContext>());

// ثبت سرویس سفر
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddSignalR();
builder.Services.AddScoped<INotificationService, NotificationService>();


// ۱. ثبت بافر هوشمند (Aggregator) - حتماً Singleton باشد
builder.Services.AddSingleton<LocationAggregator>();

// ۲. ثبت کارگر پس‌زمینه (Worker) - برای تخلیه بافر به ردیس
builder.Services.AddHostedService<LocationUploadService>();

var app = builder.Build();
app.UseRouting();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication(); // احراز هویت
app.UseAuthorization();  // سطح دسترسی

app.MapStaticAssets();
app.MapHub<RideHub>("/rideHub");



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
