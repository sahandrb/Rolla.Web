using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Rolla.Application.Interfaces;
using Rolla.Domain.Exceptions; // فرض بر این است که BusinessRuleException دارید

namespace Rolla.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    // پوشه اصلی ذخیره فایل‌ها (خارج از wwwroot)
    private readonly string _basePath;

    public LocalFileStorageService()
    {
        // فایل‌ها در پوشه Uploads در کنار پروژه ذخیره می‌شوند
        _basePath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<string> SaveFileAsync(IFormFile file, string folderName)
    {
        // 1. بررسی پسوند فایل (مرحله اول)
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

        if (!allowedExtensions.Contains(ext))
            throw new BusinessRuleException("فقط فرمت‌های تصویر (jpg, png) مجاز هستند.");

        // 2. بررسی حجم (زیر ۵ مگابایت)
        if (file.Length > 5 * 1024 * 1024)
            throw new BusinessRuleException("حجم فایل نباید بیشتر از ۵ مگابایت باشد.");

        // 3. بررسی Magic Numbers (مرحله دوم و مهم امنیتی)
        // خواندن هدر فایل برای اطمینان از اینکه واقعاً عکس است
        using (var stream = file.OpenReadStream())
        {
            var header = new byte[4];
            await stream.ReadAsync(header, 0, 4);

            // هدرهای استاندارد JPG و PNG
            bool isJpg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
            bool isPng = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;

            if (!isJpg && !isPng)
                throw new BusinessRuleException("فایل معتبر نیست (هدر فایل همخوانی ندارد).");
        }

        // 4. تغییر نام به GUID (مرحله سوم امنیتی - جلوگیری از RCE)
        var fileName = $"{Guid.NewGuid()}{ext}";
        var folderPath = Path.Combine(_basePath, folderName);

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        var fullPath = Path.Combine(folderPath, fileName);

        // 5. ذخیره نهایی
        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return fullPath;
    }

    public async Task<byte[]> GetFileBytesAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found");

        return await File.ReadAllBytesAsync(filePath);
    }

    public void DeleteFile(string filePath)
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
    }
}