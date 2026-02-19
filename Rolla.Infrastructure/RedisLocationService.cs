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
        // استفاده از متد قدیمی‌تر که با نسخه‌های زیر 6.2 ردیس هم سازگار است
        var results = await _redis.GeoRadiusAsync(RedisKey, lng, lat, radiusKm, GeoUnit.Kilometers);

        return results.Select(r => r.Member.ToString()).ToList()!;
    }
    public async Task<(double lat, double lng)?> GetDriverLocationAsync(string driverId)
    {
        // نکته: _redis خودش از قبل IDatabase است، پس مستقیماً از آن استفاده می‌کنیم.
        // همچنین از متغیر RedisKey استفاده می‌کنیم تا با بقیه متدها هماهنگ باشد.
        var pos = await _redis.GeoPositionAsync(RedisKey, driverId);

        if (pos.HasValue)
        {
            // مقدار بازگشتی از ردیس شامل Latitude و Longitude است
            return (pos.Value.Latitude, pos.Value.Longitude);
        }

        return null;
    }
}

