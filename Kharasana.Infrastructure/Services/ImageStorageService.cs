using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.Interfaces.Services;
using Microsoft.AspNetCore.Hosting;

namespace Kharasana.Infrastructure.Services;

public class ImageStorageService : IImageStorageService
{
    private readonly IWebHostEnvironment _env;

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxFileSizeInBytes = 5 * 1024 * 1024; // 5 MB

    public ImageStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> SaveImageAsync(Stream fileStream, string originalFileName, long fileLength, string folderName)
    {
        if (fileStream == null || fileLength <= 0)
            throw new BusinessException("الملف المرفوع غير صالح.");

        var extension = Path.GetExtension(originalFileName)?.ToLowerInvariant();

        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            throw new BusinessException("امتداد الملف غير مدعوم. الامتدادات المسموحة: jpg, jpeg, png, webp.");

        if (fileLength > MaxFileSizeInBytes)
            throw new BusinessException("حجم الملف يتجاوز الحد المسموح به (5 ميجابايت).");

        // ✅ استخدام Directory.GetCurrentDirectory() كحل احتياطي
        var basePath = _env.WebRootPath;

        if (string.IsNullOrEmpty(basePath))
        {
            // إذا كان WebRootPath فارغاً، استخدم مسار المشروع + wwwroot
            basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        // ✅ تأكد من وجود المجلد
        var folderPath = Path.Combine(basePath, "Images", folderName);

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(folderPath, uniqueFileName);

        await using (var output = new FileStream(fullPath, FileMode.Create))
        {
            await fileStream.CopyToAsync(output);
        }

        return $"/Images/{folderName}/{uniqueFileName}";
    }

    public void DeleteImage(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return;

        var webRootPath = _env.WebRootPath;
        if (string.IsNullOrEmpty(webRootPath))
        {
            webRootPath = Path.Combine(_env.ContentRootPath, "wwwroot");
        }

        var trimmedPath = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(webRootPath, trimmedPath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}