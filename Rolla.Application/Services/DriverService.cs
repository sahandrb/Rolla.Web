using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rolla.Application.DTOs.Auth;
using Rolla.Application.Interfaces;
using Rolla.Domain.Entities;
using Rolla.Domain.Enums;
using Rolla.Domain.Exceptions; // برای BusinessRuleException

namespace Rolla.Application.Services;

public class DriverService : IDriverService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage; // ✨ سرویس جدید فایل

    public DriverService(
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext context,
        IFileStorageService fileStorage)
    {
        _userManager = userManager;
        _context = context;
        _fileStorage = fileStorage;
    }

    // ... متدهای قبلی (Approve/Reject) سر جایشان بمانند ...

    public async Task RegisterDriverAsync(RegisterDriverDto dto, string userId)
    {
        // 1. شروع تراکنش دیتابیس (Enterprise Rule #21)
        // اگر وسط کار خطا خورد، دیتابیس کثیف نشود.
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new BusinessRuleException("کاربر یافت نشد.");

            // 2. اعتبارسنجی وضعیت
            if (user.DriverStatus == DriverStatus.Pending || user.DriverStatus == DriverStatus.Approved)
                throw new BusinessRuleException("شما قبلاً درخواست داده‌اید.");

            // 3. ذخیره فایل‌ها (توسط سرویس امن Infrastructure)
            // هر فایل در پوشه مخصوص خودش ذخیره می‌شود
            var nationalCardPath = await _fileStorage.SaveFileAsync(dto.NationalCardImage, "NationalCards");
            var licensePath = await _fileStorage.SaveFileAsync(dto.LicenseImage, "Licenses");
            var carPath = await _fileStorage.SaveFileAsync(dto.CarImage, "CarPhotos");

            // 4. آپدیت اطلاعات کاربر
            user.CarModel = dto.CarModel;
            user.PlateNumber = dto.PlateNumber;
            user.DriverStatus = DriverStatus.Pending; // وضعیت: در انتظار بررسی

            // چون از UserManager استفاده می‌کنیم، باید آپدیت را صدا بزنیم
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                throw new Exception("خطا در بروزرسانی اطلاعات کاربر.");

            // 5. ثبت مدارک در جدول DriverDocuments
            // برای هر مدرک یک رکورد می‌سازیم
            var docs = new List<DriverDocument>
            {
                new() { DriverId = userId, Type = DocumentType.NationalCard, FilePath = nationalCardPath, ContentType = dto.NationalCardImage.ContentType },
                new() { DriverId = userId, Type = DocumentType.DriverLicense, FilePath = licensePath, ContentType = dto.LicenseImage.ContentType },
                new() { DriverId = userId, Type = DocumentType.CarPhoto, FilePath = carPath, ContentType = dto.CarImage.ContentType }
            };

            _context.DriverDocuments.AddRange(docs);
            await _context.SaveChangesAsync();

            // 6. پایان موفقیت‌آمیز تراکنش
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            // اگر خطایی رخ داد، تراکنش دیتابیس رول‌بک می‌شود.
            await transaction.RollbackAsync();

            // ⚠️ نکته حرفه‌ای: اینجا می‌توانیم کدی بنویسیم که فایل‌های آپلود شده را هم پاک کند
            // (Cleanup Orphan Files) اما برای سادگی فعلاً می‌گذریم.
            throw; // خطا را به بالا پرتاب کن تا کنترلر بفهمد
        }
    }

    // ... بقیه متدها (GetPendingDriversAsync) ...
    public async Task<List<ApplicationUser>> GetPendingDriversAsync()
    {
        return await _userManager.Users
           .Where(u => u.DriverStatus == DriverStatus.Pending)
           .ToListAsync();
    }

    // ... ApproveDriverAsync ...
    // ... RejectDriverAsync ...
}