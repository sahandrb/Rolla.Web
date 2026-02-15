using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rolla.Application.Interfaces;

namespace Rolla.Application.Services
{
    public class PricingService : IPricingService
    {
        private const decimal BasePrice = 15000; // قیمت پایه ۱۵ هزار تومان
        private const decimal PricePerKm = 5000; // هر کیلومتر ۵ هزار تومان

        public decimal CalculatePrice(double originLat, double originLng, double destLat, double destLng)
        {
            // محاسبه تقریبی فاصله (خیلی ساده)
            // در واقعیت باید از Haversine استفاده شود
            var distance = Math.Sqrt(Math.Pow(destLat - originLat, 2) + Math.Pow(destLng - originLng, 2)) * 111; // تبدیل درجه به کیلومتر

            var price = BasePrice + (decimal)(distance * (double)PricePerKm);

            // رند کردن قیمت به هزار تومان
            return Math.Round(price / 1000) * 1000;
        }
    }
}