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

        // تغییر نوع بازگشتی به ?Trip (یعنی ممکن است نال باشد)
        Task<Trip?> AcceptTripAsync(int tripId, string driverId);

        // تغییر خروجی از bool به string? (که یعنی آیدی مسافر رو برمی‌گردونه)
        Task<string?> ChangeTripStatusAsync(int tripId, string driverId, Rolla.Domain.Enums.TripStatus newStatus);

        Task<bool> CancelTripAsync(int tripId, string userId);

        Task ExpandSearchRadiusAsync(int tripId);
    }
}
