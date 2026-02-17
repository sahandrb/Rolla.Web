using Microsoft.EntityFrameworkCore;
using Rolla.Application.Interfaces;
using Rolla.Domain.Enums;

namespace Rolla.Web.Services;

public class TripDispatcherService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TripDispatcherService> _logger;

    public TripDispatcherService(IServiceProvider serviceProvider, ILogger<TripDispatcherService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Trip Dispatcher Service Started...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // ایجاد اسکوپ جدید برای هر دور اجرا (چون BackgroundService سینگلتون است)
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                    var notifService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                    var geoService = scope.ServiceProvider.GetRequiredService<IGeoLocationService>();

                    // ۱. پیدا کردن تمام سفرهای در حال انتظار
                    var staleTrips = await dbContext.Trips
                        .Where(t => t.Status == TripStatus.Searching)
                        .ToListAsync(stoppingToken);

                    foreach (var trip in staleTrips)
                    {
                        var timeElapsed = DateTime.UtcNow - trip.CreatedAt;
                        double searchRadius = 2; // شعاع پیش‌فرض (اولیه)

                        // 🔴 سناریوی ۱: لغو خودکار بعد از ۳ دقیقه
                        if (timeElapsed.TotalMinutes >= 3)
                        {
                            _logger.LogWarning($"⏳ Trip {trip.Id} timed out. Canceling...");
                            trip.Status = TripStatus.Canceled;
                            await dbContext.SaveChangesAsync(stoppingToken);
                            await notifService.NotifyStatusChangeAsync(trip.Id, "Canceled");
                            continue;
                        }

                        // 🟡 سناریوی ۲: گسترش شعاع بعد از ۴۵ ثانیه
                        else if (timeElapsed.TotalSeconds > 45)
                        {
                            searchRadius = 10;
                        }

                        // 🟢 سناریوی ۳: گسترش شعاع بعد از ۱۵ ثانیه
                        else if (timeElapsed.TotalSeconds > 15)
                        {
                            searchRadius = 5;
                        }
                        else
                        {
                            // زیر ۱۵ ثانیه کاری نمی‌کنیم (چون تازه ایجاد شده و پیام اولیه رفته)
                            continue;
                        }

                        // ۲. پیدا کردن رانندگان در شعاع جدید (از Redis)
                        var nearbyDrivers = await geoService.GetNearbyDriversAsync(
                            trip.Origin.Y, trip.Origin.X, searchRadius);

                        if (!nearbyDrivers.Any()) continue;

                        // ۳. پیدا کردن رانندگانی که قبلاً رد کرده‌اند (از SQL)
                        // نکته: TripRequestLogs باید در DbContext اضافه شده باشد
                        // (چون IApplicationDbContext شامل DbSet<TripRequestLog> نیست، ممکن است اینجا خطا بدهد
                        // اگر خطا داد، باید آن را به IApplicationDbContext اضافه کنید یا کست کنید)
                        // فرض می‌کنیم اضافه شده است.

                        // اگر هنوز به IApplicationDbContext اضافه نکردی، این خط را موقتاً با var logs = ... عوض کن
                        // یا مستقیماً از dbContext واقعی استفاده کن:
                        var appDbContext = (Rolla.Infrastructure.Data.ApplicationDbContext)dbContext;

                        var rejectedDriverIds = await dbContext.TripRequestLogs
                            .AsNoTracking()
                            .Where(log => log.TripId == trip.Id && log.IsRejected)
                            .Select(log => log.DriverId)
                            .ToListAsync(stoppingToken);

                        // ۲. فیلتر کردن هوشمند (نادیده گرفتن حروف کوچک و بزرگ)
                        var eligibleDrivers = nearbyDrivers
                            .Where(d => !rejectedDriverIds.Contains(d, StringComparer.OrdinalIgnoreCase))
                            .ToList();

                        if (eligibleDrivers.Any())
                        {
                            _logger.LogInformation($"📡 Sending Trip {trip.Id} to {eligibleDrivers.Count} eligible drivers.");
                            foreach (var driverId in eligibleDrivers)
                            {
                                await notifService.NotifyDriverAsync(driverId, trip.Id, trip.Origin.Y, trip.Origin.X, trip.Price);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error in Dispatcher: {ex.Message}");
            }

            // ۶. وقفه ۱۰ ثانیه‌ای تا دور بعدی
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}