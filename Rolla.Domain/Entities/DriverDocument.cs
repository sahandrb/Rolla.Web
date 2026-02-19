using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rolla.Domain.Common;
using Rolla.Domain.Enums;

namespace Rolla.Domain.Entities;

public class DriverDocument : BaseEntity
{
    public string DriverId { get; set; } = default!;
    public string FilePath { get; set; } = default!; // مسیر فیزیکی روی دیسک (بدون دسترسی وب)
    public DocumentType Type { get; set; }

    // فقط برای نمایش به ادمین (مثلاً "image/jpeg")
    public string ContentType { get; set; } = default!;
}