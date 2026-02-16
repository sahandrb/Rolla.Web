using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using Rolla.Application.DTOs.Trip;
using Rolla.Application.Interfaces;
using Rolla.Domain.Entities;
using Rolla.Domain.Enums;



namespace Rolla.Application.Services;

public class TripService : ITripService
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService; // ۱. اضافه کردن این خط

    // ۲. اضافه کردن به سازنده
    public TripService(IApplicationDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
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
    public async Task<Trip?> AcceptTripAsync(int tripId, string driverId)
    {
        var trip = await _context.Trips.FindAsync(tripId);

        // اگر سفر نبود یا قبلاً گرفته شده بود، نال برگردان
        if (trip == null || trip.Status != TripStatus.Searching)
            return null;

        trip.DriverId = driverId;
        trip.Status = TripStatus.Accepted;

        await _context.SaveChangesAsync();

        // کل آبجکت سفر را برمی‌گردانیم تا کنترلر بتواند RiderId را از توش بردارد
        return trip;
    }
}