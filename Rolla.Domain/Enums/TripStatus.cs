using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rolla.Domain.Enums
{
    public enum TripStatus
    {
        Searching = 1, // مسافر درخواست داده و منتظر راننده است
        Accepted = 2,  // راننده سفر را قبول کرد
        Arrived = 3,   // راننده به مبدا رسید
        Started = 4,   // سفر شروع شد
        Finished = 5,  // سفر با موفقیت تمام شد
        Canceled = 6   // سفر لغو شد
    }
}
