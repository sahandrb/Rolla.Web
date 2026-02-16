using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rolla.Application.DTOs.Trip;
using Rolla.Domain.Entities; // اضافه شود

namespace Rolla.Application.Interfaces
{
    public interface ITripService
    {
        Task<int> CreateTripAsync(CreateTripDto dto, string riderId);

        // تغییر نوع بازگشتی به ?Trip (یعنی ممکن است نال باشد)
        Task<Trip?> AcceptTripAsync(int tripId, string driverId);
    }
}
