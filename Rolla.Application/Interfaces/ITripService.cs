using Rolla.Application.Common;
using Rolla.Application.DTOs.Trip;
using Rolla.Domain.Entities; // اضافه شود
using Rolla.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Rolla.Application.Interfaces
{
    public interface ITripService
    {
        Task<int> CreateTripAsync(CreateTripDto dto, string riderId);

        // تغییر: این متد خودش نوتیفیکیشن هم می‌دهد
        Task<Trip?> AcceptTripAsync(int tripId, string driverId);

        // جدید: این متد همه‌کاره برای پایان سفر (وضعیت + مالی + نوتیفیکیشن)
        Task<bool> FinishTripAsync(int tripId, string driverId);

        Task<bool> CancelTripAsync(int tripId, string userId);

        // این متد برای لاجیک بک‌گراند است (بعداً استفاده می‌کنیم)
        Task ProcessPendingTripsAsync();

        Task RejectTripAsync(int tripId, string driverId);

        Task<bool> ArriveAtOriginAsync(int tripId, string driverId);
        Task<bool> StartTripAsync(int tripId, string driverId);

        // اضافه کردن به لیست متدها
        Task<PaginatedList<TripHistoryDto>> GetTripHistoryAsync(string userId, int pageIndex, int pageSize);
        // متد ChangeTripStatusAsync قدیمی را حذف کن یا private کن چون خطرناک است مستقیم صدا زده شود
    }
}