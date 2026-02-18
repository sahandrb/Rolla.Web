using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rolla.Application.Interfaces;

public interface ITrackingService
{
    // وظیفه: پردازش لوکیشن دریافتی از راننده
    Task ProcessDriverLocationAsync(string driverId, double lat, double lng, int? tripId);
}
