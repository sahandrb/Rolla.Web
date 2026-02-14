using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rolla.Application.DTOs.Trip;

namespace Rolla.Application.Interfaces
{
    public interface ITripService
    {
        Task<int> CreateTripAsync(CreateTripDto dto, string riderId);
    }
}
