using Microsoft.EntityFrameworkCore;
using Rolla.Application.Interfaces;
using Rolla.Domain.Enums;

namespace Rolla.Web.Services;

public class TripDispatcherService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public TripDispatcherService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // ایجاد اسکوپ جدید چون BackgroundService سینگلتون است
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                    var notifService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                    var geoService = scope.ServiceProvider.GetRequiredService<IGeoLocationService>();

                    // ۱. پیدا کردن سفرهایی که بیش از ۱۵ ثانیه است منتظرند
                    var staleTrips = await dbContext.Trips
                        .Where(t => t.Status == TripStatus.Searching &&
                                    t.CreatedAt < DateTime.UtcNow.AddSeconds(-15))
                        .ToListAsync(stoppingToken);

                    foreach (var trip in staleTrips)
                    {
                        // ۲. افزایش شعاع جستجو (مثلاً ۱۰ کیلومتر)
                        var drivers = await geoService.GetNearbyDriversAsync(
                            trip.Origin.Y, trip.Origin.X, 10);

                        // ۳. ارسال مجدد
                        await notifService.NotifyNewTripAsync(trip.Id, trip.Origin.Y, trip.Origin.X, trip.Price);

                        // اینجا باید فیلدی مثل LastSearchTime را آپدیت کنیم تا دوباره ۱۰ ثانیه بعد این سفر را نگیریم
                        // اما برای سادگی فعلاً لاگ می‌زنیم
                        Console.WriteLine($"Expanding search for Trip {trip.Id} to 10km");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Dispatcher: {ex.Message}");
            }

            // هر ۱۰ ثانیه اجرا شود
            await Task.Delay(10000, stoppingToken);
        }
    }
}