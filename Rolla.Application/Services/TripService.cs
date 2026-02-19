using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using Rolla.Application.Common;
using Rolla.Application.DTOs.Trip;
using Rolla.Application.Interfaces;
using Rolla.Domain.Entities;
using Rolla.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace Rolla.Application.Services;

public class TripService : ITripService
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IGeoLocationService _geoLocationService; // 👈 ۱. این خط رو اضافه کن                                                   // متغیر جدید اضافه کن:
    private readonly IWalletService _walletService;

    // سازنده را تغییر بده:
    public TripService(
        IApplicationDbContext context,
        INotificationService notificationService,
        IGeoLocationService geoLocationService,
        IWalletService walletService) // ✅ جدید
    {
        _context = context;
        _notificationService = notificationService;
        _geoLocationService = geoLocationService;
        _walletService = walletService; // ✅ جدید
    }
    public async Task<int> CreateTripAsync(CreateTripDto dto, string riderId)
    {
        var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var originPoint = geometryFactory.CreatePoint(new NetTopologySuite.Geometries.Coordinate(dto.OriginLng, dto.OriginLat));
        var destPoint = geometryFactory.CreatePoint(new NetTopologySuite.Geometries.Coordinate(dto.DestinationLng, dto.DestinationLat));

        var trip = new Trip
        {
            RiderId = riderId,
            Origin = originPoint,
            Destination = destPoint,
            Price = dto.EstimatedPrice,
            Status = TripStatus.Searching,
            CreatedAt = DateTime.UtcNow
        };

        _context.Trips.Add(trip);
        await _context.SaveChangesAsync();

        // ۱. پیدا کردن رانندگان از ردیس
        var nearbyDrivers = await _geoLocationService.GetNearbyDriversAsync(dto.OriginLat, dto.OriginLng, 5);

        // ۲. ارسال نوتیفیکیشن به رانندگان انتخاب شده
        await _notificationService.NotifyNewTripAsync(nearbyDrivers, trip.Id, dto.OriginLat, dto.OriginLng, trip.Price);

        return trip.Id;
    }
    // فایل: Rolla.Application/Services/TripService.cs

    public async Task<Trip?> AcceptTripAsync(int tripId, string driverId)
    {
        try
        {
            var trip = await _context.Trips.FindAsync(tripId);
            // لاجیک چک کردن
            if (trip == null || trip.Status != TripStatus.Searching) return null;

            // لاجیک تغییر وضعیت
            trip.DriverId = driverId;
            trip.Status = TripStatus.Accepted;
            await _context.SaveChangesAsync();

            // ✅ انتقال نوتیفیکیشن به اینجا (دیگر کنترلر نباید بفرستد)
            await _notificationService.NotifyTripAcceptedAsync(trip.Id, trip.RiderId, driverId);

            return trip;
        }
        catch (DbUpdateConcurrencyException)
        {
            return null;
        }
    }
    public async Task<bool> CancelTripAsync(int tripId, string userId)
    {
        var trip = await _context.Trips.FindAsync(tripId);

        if (trip == null) return false;

        // فقط راننده یا مسافر همین سفر یا ادمین اجازه لغو دارند
        if (trip.RiderId != userId && trip.DriverId != userId) return false;

        // اگر سفر تمام شده باشد نمی‌توان لغو کرد
        if (trip.Status == TripStatus.Finished) return false;

        trip.Status = TripStatus.Canceled;
        await _context.SaveChangesAsync();

        // اینجا می‌توانی منطق جریمه را هم اضافه کنی (مثلاً اگر راننده داشت می‌آمد و لغو شد)

        // ارسال نوتیفیکیشن به طرف مقابل
        string targetId = (userId == trip.RiderId) ? trip.DriverId : trip.RiderId;
        if (targetId != null)
        {
            await _notificationService.NotifyStatusChangeAsync(trip.Id, "Canceled");
        }

        return true;
    }





    public async Task<string?> ChangeTripStatusAsync(int tripId, string driverId, Rolla.Domain.Enums.TripStatus newStatus)
    {
        var trip = await _context.Trips.FindAsync(tripId);

        // اگر سفر نبود یا راننده اشتباه بود، نال برگردان
        if (trip == null || trip.DriverId != driverId) return null;

        trip.Status = newStatus;
        await _context.SaveChangesAsync();

        // ✨ آیدی مسافر رو برمی‌گردونیم تا کنترلر بتونه بهش نوتیفیکیشن بده
        return trip.RiderId;
    }

    public async Task RejectTripAsync(int tripId, string driverId)
    {
        // ثبت اینکه راننده این سفر را رد کرده است
        var log = new TripRequestLog
        {
            TripId = tripId,
            DriverId = driverId,
            IsRejected = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.TripRequestLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task ExpandSearchRadiusAsync(int tripId)
    {
        var trip = await _context.Trips.FindAsync(tripId);
        if (trip == null || trip.Status != TripStatus.Searching) return;

        // فعلاً شعاع را ثابت ۵ کیلومتر می‌گیریم (مرحله دوم)
        // در واقعیت باید یک فیلد `CurrentSearchRadius` در دیتابیس داشته باشیم
        // و هر بار آن را افزایش دهیم (2 -> 5 -> 10)

        // ۱. پیدا کردن رانندگان جدید
        // ۱. رانندگان جدید را پیدا کن
        var nearbyDrivers = await _geoLocationService.GetNearbyDriversAsync(trip.Origin.Y, trip.Origin.X, 5);

        // ۲. به جای حلقه، کل لیست را یکجا به متد بفرست (طبق امضای جدید)
        await _notificationService.NotifyNewTripAsync(nearbyDrivers, trip.Id, trip.Origin.Y, trip.Origin.X, trip.Price);
    }



    public async Task<bool> FinishTripAsync(int tripId, string driverId)
    {
        // ۱. شروع یک تراکنش واقعی در دیتابیس
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var trip = await _context.Trips.FindAsync(tripId);

            if (trip == null || trip.DriverId != driverId) return false;
            if (trip.Status == TripStatus.Finished) return true;

            // ۲. تغییر وضعیت سفر (در حافظه)
            trip.Status = TripStatus.Finished;

            // ۳. انجام عملیات مالی
            // نکته: این سرویس خودش SaveChanges می‌زند اما چون داخل تراکنش هستیم، نهایی نمی‌شود
            if (trip.Price > 0)
            {
                await _walletService.ProcessTripPaymentAsync(tripId, trip.RiderId, driverId, trip.Price);
            }

            // ۴. ذخیره وضعیت سفر
            await _context.SaveChangesAsync();

            // ۵. پایان موفقیت‌آمیز تراکنش (تایید نهایی هر دو مرحله)
            await transaction.CommitAsync();

            // ۶. ارسال نوتیفیکیشن (بعد از اطمینان از تراکنش دیتابیس)
            await _notificationService.NotifyStatusChangeAsync(tripId, "Finished");

            return true;
        }
        catch (Exception)
        {
            // ۷. اگر هر خطایی در هر مرحله‌ای رخ داد، همه چیز را لغو کن
            await transaction.RollbackAsync();
            return false;
        }
    }
    public async Task ProcessPendingTripsAsync()
    {
        var staleTrips = await _context.Trips
            .Where(t => t.Status == TripStatus.Searching)
            .ToListAsync();

        foreach (var trip in staleTrips)
        {
            var timeElapsed = DateTime.UtcNow - trip.CreatedAt;

            // لاجیک زمان‌بندی (Business Rules)
            if (timeElapsed.TotalMinutes >= 3)
            {
                trip.Status = TripStatus.Canceled;
                await _notificationService.NotifyStatusChangeAsync(trip.Id, "Canceled");
            }
            else
            {
                // لاجیک گسترش شعاع
                double radius = 2;
                if (timeElapsed.TotalSeconds > 45) radius = 10;
                else if (timeElapsed.TotalSeconds > 15) radius = 5;
                // پیدا کردن رانندگان
                var drivers = await _geoLocationService.GetNearbyDriversAsync(trip.Origin.Y, trip.Origin.X, radius);

                if (drivers.Any())
                {
                    // ارسال گروهی نوتیفیکیشن
                    await _notificationService.NotifyNewTripAsync(drivers, trip.Id, trip.Origin.Y, trip.Origin.X, trip.Price);
                }
            
            }
        }
        await _context.SaveChangesAsync();
    }
    public async Task<bool> ArriveAtOriginAsync(int tripId, string driverId)
    {
        var trip = await _context.Trips.FindAsync(tripId);

        // ۱. اعتبارسنجی: فقط راننده خودش و فقط اگر وضعیت Accepted باشد
        if (trip == null || trip.DriverId != driverId || trip.Status != TripStatus.Accepted)
        {
            return false;
        }

        // ۲. تغییر وضعیت
        trip.Status = TripStatus.Arrived;
        await _context.SaveChangesAsync();

        // ۳. ارسال نوتیفیکیشن (داخل سرویس)
        await _notificationService.NotifyStatusChangeAsync(trip.Id, "Arrived");

        return true;
    }

    public async Task<bool> StartTripAsync(int tripId, string driverId)
    {
        var trip = await _context.Trips.FindAsync(tripId);

        // ۱. اعتبارسنجی: فقط اگر وضعیت Arrived باشد می‌توان سفر را شروع کرد
        if (trip == null || trip.DriverId != driverId || trip.Status != TripStatus.Arrived)
        {
            return false;
        }

        // ۲. تغییر وضعیت و ثبت زمان شروع (اختیاری)
        trip.Status = TripStatus.Started;
        // trip.StartTime = DateTime.UtcNow; // اگر فیلدش را داری

        await _context.SaveChangesAsync();

        // ۳. ارسال نوتیفیکیشن
        await _notificationService.NotifyStatusChangeAsync(trip.Id, "Started");

        return true;
    }

    public async Task<PaginatedList<TripHistoryDto>> GetTripHistoryAsync(string userId, int pageIndex, int pageSize)
    {
        // ۱. ایجاد کوئری پایه (بدون اجرا)
        // AsNoTracking باعث می‌شود حافظه رم سرور اشغال نشود (Read-Only)
        var query = _context.Trips
            .AsNoTracking()
            .Where(t => t.RiderId == userId || t.DriverId == userId)
            .OrderByDescending(t => t.CreatedAt);

        // ۲. محاسبه تعداد کل برای صفحه‌بندی
        var count = await query.CountAsync();

        // ۳. پروجکشن (Projection): فقط ستون‌های مورد نیاز را از دیتابیس می‌کشیم
        // این کار باعث می‌شود ستون‌های سنگین جغرافیا (Spatial) لود نشوند.
        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TripHistoryDto
            {
                Id = t.Id,
                CreatedAt = t.CreatedAt,
                Price = t.Price,
                Status = t.Status.ToString(),
                Role = t.DriverId == userId ? "راننده" : "مسافر",
                // محاسبه مبلغ خالص در سطح دیتابیس (SQL)
                NetAmount = t.DriverId == userId ? (t.Price * 0.8m) : t.Price
            })
            .ToListAsync();

        return new PaginatedList<TripHistoryDto>(items, count, pageIndex, pageSize);
    }
}