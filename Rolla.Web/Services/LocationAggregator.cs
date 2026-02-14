using System.Collections.Concurrent;
using Rolla.Application.Interfaces;

namespace Rolla.Web.Services;

public class LocationAggregator
{
    // این لایه اول ذخیره‌سازی است: فوق‌سریع در RAM سرور
    private readonly ConcurrentDictionary<string, (double lat, double lng)> _buffer = new();
    private readonly IServiceProvider _serviceProvider;

    public LocationAggregator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    // راننده‌ها مختصات رو به این متد شلیک می‌کنند
    public void AddLocation(string driverId, double lat, double lng)
    {
        _buffer[driverId] = (lat, lng);
    }

    // کارگر پس‌زمینه (Background Worker) این متد رو صدا می‌زنه
    public async Task FlushToRedisAsync()
    {
        if (_buffer.IsEmpty) return;

        // چون Aggregator سینگلتون هست، برای استفاده از سرویس‌های Scoped مثل GeoService باید اسکوپ بسازیم
        using var scope = _serviceProvider.CreateScope();
        var geoService = scope.ServiceProvider.GetRequiredService<IGeoLocationService>();

        // تخلیه بافر به ردیس به صورت دسته‌ای
        foreach (var item in _buffer)
        {
            await geoService.UpdateDriverLocationAsync(item.Key, item.Value.lat, item.Value.lng);
            _buffer.TryRemove(item.Key, out _);
        }
    }
}