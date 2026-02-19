using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Rolla.Application.DTOs.Trip;

public class RouteResponseDto
{
    // رشته فشرده مختصات برای رسم روی نقشه (Polyline)
    public string EncodedPolyline { get; set; } = default!;

    // مسافت بر حسب متر
    public double DistanceMeters { get; set; }

    // زمان تقریبی بر حسب ثانیه
    public double DurationSeconds { get; set; }
}