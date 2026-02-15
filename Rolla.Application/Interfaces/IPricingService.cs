using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rolla.Application.Interfaces
{
    public interface IPricingService
    {
        decimal CalculatePrice(double originLat, double originLng, double destLat, double destLng);
    }
}
