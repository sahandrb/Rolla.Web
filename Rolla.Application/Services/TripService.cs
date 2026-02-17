using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
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
    private readonly IGeoLocationService _geoLocationService; // 👈 ۱. این خط رو اضافه کن

    // 👈 ۲. سازنده رو آپدیت کن (پارامتر سوم رو اضافه کن)
    public TripService(
        IApplicationDbContext context,
        INotificationService notificationService,
        IGeoLocationService geoLocationService)
    {
        _context = context;
        _notificationService = notificationService;
        _geoLocationService = geoLocationService; // 👈 ۳. مقداردهی کن
    }

    public async Task<int> CreateTripAsync(CreateTripDto dto, string riderId)
    {
        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var originPoint = geometryFactory.CreatePoint(new Coordinate(dto.OriginLng, dto.OriginLat));
        var destPoint = geometryFactory.CreatePoint(new Coordinate(dto.DestinationLng, dto.DestinationLat));

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

        // ۳. شلیک نوتیفیکیشن زنده به SignalR
        await _notificationService.NotifyNewTripAsync(trip.Id, dto.OriginLat, dto.OriginLng, trip.Price);

        return trip.Id;
    }
    // فایل: Rolla.Application/Services/TripService.cs

    public async Task<Trip?> AcceptTripAsync(int tripId, string driverId)
    {
        try
        {
            var trip = await _context.Trips.FindAsync(tripId);

            // ۱. چک کردن منطقی
            if (trip == null || trip.Status != TripStatus.Searching)
                return null; // یعنی سفر قبلاً گرفته شده یا کنسل شده

            // ۲. اختصاص راننده
            trip.DriverId = driverId;
            trip.Status = TripStatus.Accepted;

            // ۳. ذخیره با کنترل همزمانی
            // اگر در فاصله بین FindAsync و SaveChangesAsync، راننده دیگری این رکورد را تغییر داده باشد،
            // EF Core خطای DbUpdateConcurrencyException می‌دهد.
            await _context.SaveChangesAsync();

            return trip;
        }
        catch (DbUpdateConcurrencyException)
        {
            // یعنی یک نفر دیگه زودتر دکمه رو زده و رکورد رو تغییر داده
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
        var nearbyDrivers = await _geoLocationService.GetNearbyDriversAsync(
            trip.Origin.Y, trip.Origin.X, 5); // شعاع ۵ کیلومتر

        // ۲. ارسال مجدد نوتیفیکیشن
        foreach (var driverId in nearbyDrivers)
        {
            await _notificationService.NotifyNewTripAsync(trip.Id, trip.Origin.Y, trip.Origin.X, trip.Price);
        }
    }
}