using Rolla.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rolla.Application.DTOs.Auth; 

namespace Rolla.Application.Interfaces;

public interface IDriverService
{
    // دریافت لیست رانندگان منتظر
    Task<List<ApplicationUser>> GetPendingDriversAsync();

    // تایید راننده
    Task<bool> ApproveDriverAsync(string userId);

    // رد راننده
    Task<bool> RejectDriverAsync(string userId);

    Task RegisterDriverAsync(RegisterDriverDto dto, string userId);
}