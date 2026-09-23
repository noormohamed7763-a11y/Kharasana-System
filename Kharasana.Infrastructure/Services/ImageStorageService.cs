using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.Interfaces.Services;
using Microsoft.AspNetCore.Hosting;

namespace Kharasana.Infrastructure.Services;

public class ImageStorageService : IImageStorageService
{
    private readonly IWebHostEnvironment _env;

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly Dictionary<string, string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".webp", "image/webp" }
    };
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

        if (!IsValidMimeType(fileStream, extension))
            throw new BusinessException("نوع محتوى الملف غير صالح أو لا يتطابق مع الامتداد.");

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

    private bool IsValidMimeType(Stream fileStream, string extension)
    {
        if (!AllowedMimeTypes.ContainsKey(extension))
            return false;

        try
        {
            using var reader = new BinaryReader(fileStream, System.Text.Encoding.UTF8, leaveOpen: true);
            var headerBytes = reader.ReadBytes(12);
            fileStream.Position = 0;

            return extension switch
            {
                ".png" => headerBytes[0] == 0x89 && headerBytes[1] == 0x50 && headerBytes[2] == 0x4E && headerBytes[3] == 0x47,
                ".jpg" or ".jpeg" => headerBytes[0] == 0xFF && headerBytes[1] == 0xD8,
                ".webp" => headerBytes[0] == 0x52 && headerBytes[1] == 0x49 && headerBytes[2] == 0x46 && headerBytes[3] == 0x46,
                _ => false
            };
        }
        catch
        {
            return false;
        }
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