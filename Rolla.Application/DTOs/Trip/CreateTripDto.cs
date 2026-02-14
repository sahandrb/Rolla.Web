using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rolla.Application.DTOs.Trip
{

    public class CreateTripDto
    {

        // مختصات به صورت عدد اعشاری از فرانت‌اِند می‌آید
        public double OriginLat { get; set; }
        public double OriginLng { get; set; }
        public double DestinationLat { get; set; }
        public double DestinationLng { get; set; }
        public decimal EstimatedPrice { get; set; }
    }
}


