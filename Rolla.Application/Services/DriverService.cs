using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rolla.Application.DTOs.Admin;
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
    // ... ادامه کدهای قبلی ...

    public async Task<List<ApplicationUser>> GetPendingDriversAsync()
    {
        // این کد تمام کسانی که وضعیت رانندگی دارند (تایید، رد، در انتظار) را می‌آورد
        return await _userManager.Users
           .Where(u => u.DriverStatus != Rolla.Domain.Enums.DriverStatus.None)
           .OrderBy(u => u.DriverStatus == Rolla.Domain.Enums.DriverStatus.Pending ? 0 : 1) // اولویت با در انتظارها
           .ToListAsync();
    }

    public async Task<bool> ApproveDriverAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        user.DriverStatus = DriverStatus.Approved;
        user.IsDriver = true;
        await _userManager.UpdateAsync(user);

        if (!await _userManager.IsInRoleAsync(user, "Driver"))
            await _userManager.AddToRoleAsync(user, "Driver");

        return true;
    }

    public async Task<bool> RejectDriverAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        user.DriverStatus = DriverStatus.Rejected;
        await _userManager.UpdateAsync(user);
        return true;
    }

    public async Task<DriverDetailsDto?> GetDriverDetailsAsync(string driverId)
    {
        // گرفتن کاربر به همراه مدارکش از دیتابیس
        var user = await _context.Users
            .Include(u => u.Documents)
            .FirstOrDefaultAsync(u => u.Id == driverId);

        if (user == null) return null;

        // مپ کردن به DTO (بدون ارسال مسیر فیزیکی فایل به لایه وب)
        return new DriverDetailsDto
        {
            Id = user.Id,
            Email = user.Email!,
            FullName = user.FullName,
            CarModel = user.CarModel ?? "نامشخص",
            PlateNumber = user.PlateNumber ?? "نامشخص",
            Status = user.DriverStatus,
            Documents = user.Documents.Select(d => new DriverDocumentDto
            {
                Id = d.Id,
                Type = d.Type,
                ContentType = d.ContentType
            }).ToList()
        };
    }

    public async Task<(byte[] FileBytes, string ContentType)?> GetDocumentFileAsync(int documentId)
    {
        var document = await _context.DriverDocuments.FindAsync(documentId);
        if (document == null) return null;

        // خواندن امن فایل از طریق سرویس فایل
        var bytes = await _fileStorage.GetFileBytesAsync(document.FilePath);

        return (bytes, document.ContentType);
    }
}
