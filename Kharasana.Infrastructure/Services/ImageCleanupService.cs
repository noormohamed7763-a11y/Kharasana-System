using Kharasana.Application.Interfaces.Services;
using Kharasana.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kharasana.Infrastructure.Services;

public class ImageCleanupService : IImageCleanupService
{
    private readonly IWebHostEnvironment _env;
    private readonly KharasanaDbContext _context;
    private readonly ILogger<ImageCleanupService> _logger;

    public ImageCleanupService(
        IWebHostEnvironment env,
        KharasanaDbContext context,
        ILogger<ImageCleanupService> logger)
    {
        _env = env;
        _context = context;
        _logger = logger;
    }

    public async Task<int> CleanupOrphanedImagesAsync()
    {
        var orphanedFiles = await FindOrphanedImagesAsync();

        if (orphanedFiles.Count == 0)
            return 0;

        var imagesRoot = GetImagesRoot();

        foreach (var relativePath in orphanedFiles)
        {
            try
            {
                var fullPath = Path.Combine(imagesRoot, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                var resolvedPath = Path.GetFullPath(fullPath);

                if (resolvedPath.StartsWith(imagesRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(resolvedPath))
                {
                    File.Delete(resolvedPath);
                    _logger.LogInformation("تم حذف ملف صورة يتيم: {Path}", relativePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "فشل حذف ملف صورة يتيم: {Path}", relativePath);
            }
        }

        return orphanedFiles.Count;
    }

    public async Task<List<string>> FindOrphanedImagesAsync()
    {
        var imagesRoot = GetImagesRoot();
        var orphanedFiles = new List<string>();

        if (!Directory.Exists(imagesRoot))
            return orphanedFiles;

        // جلب المسارات المسجلة من قاعدة البيانات
        var knownPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var factoryLogos = await _context.Factories
            .IgnoreQueryFilters()
            .Where(f => f.Logo != null && !f.IsDeleted)
            .Select(f => f.Logo)
            .ToListAsync();

        foreach (var logo in factoryLogos.Where(l => !string.IsNullOrWhiteSpace(l)))
            knownPaths.Add(logo!.TrimStart('/'));

        var userImages = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.ProfileImage != null && !u.IsDeleted)
            .Select(u => u.ProfileImage)
            .ToListAsync();

        foreach (var image in userImages.Where(i => !string.IsNullOrWhiteSpace(i)))
            knownPaths.Add(image!.TrimStart('/'));

        // فحص الملفات على القرص
        var allFilesOnDisk = Directory.EnumerateFiles(imagesRoot, "*.*", SearchOption.AllDirectories)
            .Select(p => p.Substring(imagesRoot.Length).TrimStart(Path.DirectorySeparatorChar).Replace(Path.DirectorySeparatorChar, '/'))
            .ToList();

        foreach (var diskPath in allFilesOnDisk)
        {
            if (!knownPaths.Contains(diskPath))
                orphanedFiles.Add(diskPath);
        }

        return orphanedFiles;
    }

    private string GetImagesRoot()
    {
        var webRootPath = string.IsNullOrEmpty(_env.WebRootPath)
            ? Path.Combine(_env.ContentRootPath, "wwwroot")
            : _env.WebRootPath;

        return Path.Combine(webRootPath, "Images");
    }
}
