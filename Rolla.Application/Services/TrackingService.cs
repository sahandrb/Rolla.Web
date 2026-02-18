using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rolla.Application.Interfaces;

namespace Rolla.Application.Services;

public class TrackingService : ITrackingService
{
    private readonly IGeoLocationService _geoLocationService;
    private readonly INotificationService _notificationService;

    public TrackingService(
        IGeoLocationService geoLocationService,
        INotificationService notificationService)
    {
        _geoLocationService = geoLocationService;
        _notificationService = notificationService;
    }

    public async Task ProcessDriverLocationAsync(string driverId, double lat, double lng, int? tripId)
    {
        // ۱. همیشه لوکیشن را در سیستم (Redis) ذخیره کن
        // (حتی اگر راننده مسافر نداشته باشد، باید برای جستجوهای بعدی ذخیره شود)
        await _geoLocationService.UpdateDriverLocationAsync(driverId, lat, lng);

        // ۲. اگر راننده در حال سفر است، لوکیشن را برای مسافر هم بفرست
        if (tripId.HasValue)
        {
            // اینجا لاجیک "ارسال زنده" است
            await _notificationService.NotifyDriverLocationToRiderAsync(tripId.Value, lat, lng);
        }
    }
}