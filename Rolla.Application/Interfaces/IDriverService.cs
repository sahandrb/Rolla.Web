using Rolla.Application.DTOs.Admin;
using Rolla.Application.DTOs.Auth; 
using Rolla.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rolla.Application.Interfaces;

public interface IDriverService
{
    // دریافت لیست رانندگان منتظر
    Task<List<ApplicationUser>> GetPendingDriversAsync();

    // تایید راننده
    Task<bool> ApproveDriverAsync(string userId);

    // رد راننده
    Task<bool> RejectDriverAsync(string userId);

    // دریافت جزئیات راننده برای صفحه بررسی
    Task<DriverDetailsDto?> GetDriverDetailsAsync(string driverId);

    // دریافت فایل فیزیکی عکس برای نمایش به ادمین
    Task<(byte[] FileBytes, string ContentType)?> GetDocumentFileAsync(int documentId);
    Task RegisterDriverAsync(RegisterDriverDto dto, string userId);
}