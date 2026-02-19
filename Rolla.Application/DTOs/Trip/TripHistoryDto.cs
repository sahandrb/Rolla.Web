using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rolla.Application.DTOs.Trip;

public class TripHistoryDto
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal Price { get; set; }        // مبلغ کل سفر
    public decimal NetAmount { get; set; }    // مبلغ خالص (برای راننده ۸۰٪، برای مسافر ۱۰۰٪)
    public string Role { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string DestinationAddress { get; set; } // در صورت تمایل به ذخیره متنی آدرس

    public string StatusPersian => Status switch
    {
        "Searching" => "در جستجوی راننده",
        "Accepted" => "پذیرفته شده",
        "Arrived" => "راننده در مبدا",
        "Started" => "در حال سفر",
        "Finished" => "پایان یافته ✅",
        "Canceled" => "لغو شده ❌",
        _ => Status
    };

}

