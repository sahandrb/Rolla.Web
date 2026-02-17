using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rolla.Domain.Entities
{

    public class Trip
    {

        public int Id { get; set; }
        public string RiderId { get; set; } = default!; // آیدی مسافر از Identity
        public string? DriverId { get; set; }           // آیدی راننده (ابتدا نال است)

        public Point Origin { get; set; } = default!;      // مبدا
        public Point Destination { get; set; } = default!; // مقصد

        public decimal Price { get; set; }
        public Enums.TripStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Timestamp] // این اتریبیوت جادو می‌کند!
        public byte[] RowVersion { get; set; } = default!;
    }
}
