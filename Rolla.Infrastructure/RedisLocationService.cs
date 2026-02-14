using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using Rolla.Application.Interfaces;

namespace Rolla.Infrastructure.Services;

public class RedisLocationService : IGeoLocationService
{
    private readonly IDatabase _redis;
    private const string RedisKey = "drivers_locations"; // کلید مخصوص راننده‌ها در ردیس

    public RedisLocationService(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    public async Task UpdateDriverLocationAsync(string driverId, double lat, double lng)
    {
        // استفاده از دستور GEOADD در ردیس (بسیار سریع‌تر از SQL)
        await _redis.GeoAddAsync(RedisKey, lng, lat, driverId);
    }

    public async Task<List<string>> GetNearbyDriversAsync(double lat, double lng, double radiusKm)
    {
        // تعریف شکل جستجو (دایره‌ای با شعاع مشخص)
        var shape = new GeoSearchCircle(radiusKm, GeoUnit.Kilometers);

        // انجام جستجو: 
        // ورودی دوم: Longitude (lng)
        // ورودی سوم: Latitude (lat)
        // ورودی چهارم: شکل جستجو (shape)
        var results = await _redis.GeoSearchAsync(RedisKey, lng, lat, shape);

        return results.Select(r => r.Member.ToString()).ToList()!;
    }
}