using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Text.Json;
using Rolla.Application.DTOs.Trip;
using Rolla.Application.Interfaces;

namespace Rolla.Infrastructure.Services;

public class OsrmRoutingService : IRoutingService
{
    private readonly HttpClient _httpClient;

    public OsrmRoutingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        // تنظیم User-Agent الزامی است برای APIهای رایگان تا بلاک نشوید
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "RollaApp-TaxiService");
    }

    public async Task<RouteResponseDto?> GetRouteAsync(double startLat, double startLng, double endLat, double endLng)
    {
        try
        {
            // فرمت OSRM: {lng},{lat};{lng},{lat}
            // پارامتر overview=full برای گرفتن Polyline دقیق است
            var url = $"https://router.project-osrm.org/route/v1/driving/{startLng},{startLat};{endLng},{endLat}?overview=full&geometries=polyline";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode) return null;

            // استخراج داده‌ها از JSON پیچیده OSRM
            using var jsonDoc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var root = jsonDoc.RootElement;

            if (root.GetProperty("code").GetString() != "Ok") return null;

            var route = root.GetProperty("routes")[0];

            return new RouteResponseDto
            {
                EncodedPolyline = route.GetProperty("geometry").GetString() ?? "",
                DistanceMeters = route.GetProperty("distance").GetDouble(),
                DurationSeconds = route.GetProperty("duration").GetDouble()
            };
        }
        catch (Exception)
        {
            // در لایه زیرساخت فقط خطا را مدیریت می‌کنیم یا لاگ می‌اندازیم
            return null;
        }
    }
}