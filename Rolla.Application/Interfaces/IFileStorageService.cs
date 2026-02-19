using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Rolla.Application.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// فایل را به صورت امن ذخیره می‌کند و مسیر آن را برمی‌گرداند
    /// </summary>
    Task<string> SaveFileAsync(IFormFile file, string folderName);

    /// <summary>
    /// فایل را برای دانلود/نمایش می‌خواند
    /// </summary>
    Task<byte[]> GetFileBytesAsync(string filePath);

    /// <summary>
    /// حذف فایل (مثلاً در صورت رد شدن مدارک)
    /// </summary>
    void DeleteFile(string filePath);
}