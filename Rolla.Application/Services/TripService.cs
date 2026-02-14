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

    public TripService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateTripAsync(CreateTripDto dto, string riderId)
    {
        // ایجاد کارخانه برای ساخت نقاط جغرافیایی با استاندارد GPS (SRID 4326)
        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        // تبدیل اعداد اعشاری به مدل مختصاتی Point
        var originPoint = geometryFactory.CreatePoint(new Coordinate(dto.OriginLng, dto.OriginLat));
        var destPoint = geometryFactory.CreatePoint(new Coordinate(dto.DestinationLng, dto.DestinationLat));

        var trip = new Trip
        {
            RiderId = riderId,
            Origin = originPoint,
            Destination = destPoint,
            Price = dto.EstimatedPrice,
            Status = TripStatus.Searching, // وضعیت شروع: در حال جستجو برای راننده
            CreatedAt = DateTime.UtcNow
        };

        _context.Trips.Add(trip);
        await _context.SaveChangesAsync();

        return trip.Id; // برگرداندن آیدی سفر برای استفاده در SignalR یا مانیتورینگ
    }
}
