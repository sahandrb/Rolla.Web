using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rolla.Domain.Common;

namespace Rolla.Domain.Entities;

public class TripRequestLog : BaseEntity
{
    public int TripId { get; set; }
    public string DriverId { get; set; } = default!;
    public bool IsRejected { get; set; } // آیا راننده رد کرده؟
}