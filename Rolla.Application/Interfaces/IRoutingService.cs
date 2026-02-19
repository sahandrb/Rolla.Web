using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rolla.Application.DTOs.Trip;

namespace Rolla.Application.Interfaces;

public interface IRoutingService
{
    /// <summary>
    /// محاسبه مسیر بین دو نقطه جغرافیایی
    /// </summary>
    Task<RouteResponseDto?> GetRouteAsync(double startLat, double startLng, double endLat, double endLng);
}