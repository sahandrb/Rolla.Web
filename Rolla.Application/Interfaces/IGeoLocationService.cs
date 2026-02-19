using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rolla.Application.Interfaces;

public interface IGeoLocationService
{
    // ۱. راننده لوکیشنش رو اینجا آپدیت می‌کنه
    Task UpdateDriverLocationAsync(string driverId, double lat, double lng);
    // دریافت آخرین لوکیشن ثبت شده یک راننده خاص
    Task<(double lat, double lng)?> GetDriverLocationAsync(string driverId);
    // ۲. سیستم می‌پرسه: کی دور و برِ این مختصات هست؟ (مثلاً شعاع ۵ کیلومتری)
    Task<List<string>> GetNearbyDriversAsync(double lat, double lng, double radiusKm);
}