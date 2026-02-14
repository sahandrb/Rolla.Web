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
}