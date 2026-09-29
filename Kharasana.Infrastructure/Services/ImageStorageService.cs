using Kharasana.Application.Common;
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
            throw new BusinessException(Messages.InvalidUploadedFile);

        var extension = Path.GetExtension(originalFileName)?.ToLowerInvariant();

        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            throw new BusinessException(Messages.UnsupportedFileExtension);

        if (fileLength > MaxFileSizeInBytes)
            throw new BusinessException(Messages.FileTooLarge);

        if (!IsValidMimeType(fileStream, extension))
            throw new BusinessException(Messages.InvalidFileContentType);

        // ✅ حارس اجتياز المسار على اسم المجلد: الوسيط جزء من عقد الواجهة، ولو مرّره
        //    مستدعٍ من مدخلات المستخدم لصار ".." أو مسار مطلق يكتب خارج مجلد الصور.
        //    المستدعي الحالي يمرّر "Factories" ثابتة — الحارس يمنع الانحدار مستقبلاً.
        if (string.IsNullOrWhiteSpace(folderName)
            || folderName.Contains("..", StringComparison.Ordinal)
            || folderName.IndexOfAny('/', '\\') >= 0
            || Path.IsPathRooted(folderName))
        {
            throw new BusinessException(Messages.InvalidStorageFolder);
        }

        // ✅ مسار wwwroot موحّد بين الحفظ والحذف — كان الحفظ يستخدم GetCurrentDirectory()
        //    والحذف ContentRootPath، فيختلف المسار المُحلَّل بينهما عند غياب WebRootPath
        //    (يُحفظ الملف في مكان ويُبحث عنه في آخر).
        var basePath = ResolveWebRootPath();

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

            // ملف أقصر من 4 بايت لا يكون صورة بصيغة مدعومة أصلاً.
            if (headerBytes.Length < 4)
                return false;

            return extension switch
            {
                ".png" => headerBytes[0] == 0x89 && headerBytes[1] == 0x50 && headerBytes[2] == 0x4E && headerBytes[3] == 0x47,
                ".jpg" or ".jpeg" => headerBytes[0] == 0xFF && headerBytes[1] == 0xD8,
                // ✅ "RIFF" وحدها لا تكفي: كل ملفات AVI و WAV تبدأ بها أيضاً، فكان ملف
                //    فيديو مُعاد تسميته إلى ‎.webp‎ يمرّ من فحص البصمة. بصمة WebP الحقيقية
                //    هي "RIFF" في 0..3 و"WEBP" في 8..11 — وأي ملف WebP سليم لا يقل عن 12 بايت.
                ".webp" => headerBytes.Length >= 12
                           && headerBytes[0] == 0x52 && headerBytes[1] == 0x49 && headerBytes[2] == 0x46 && headerBytes[3] == 0x46
                           && headerBytes[8] == 0x57 && headerBytes[9] == 0x45 && headerBytes[10] == 0x42 && headerBytes[11] == 0x50,
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

        var webRootPath = ResolveWebRootPath();

        // ✅ حارس اجتياز المسار (path traversal): المسار المحلَّل يجب أن يبقى داخل
        //    wwwroot/Images. المصدر غير موثوق — factory.Logo يأتي من حقل يُدخله المستخدم
        //    عند إنشاء المصنع، ومسار مثل "../../appsettings.json" كان يحذف ملفًا خارج المجلد.
        var imagesRoot = Path.GetFullPath(Path.Combine(webRootPath, "Images"));
        var imagesRootPrefix = imagesRoot.EndsWith(Path.DirectorySeparatorChar)
            ? imagesRoot
            : imagesRoot + Path.DirectorySeparatorChar;

        var trimmedPath = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(webRootPath, trimmedPath));

        // خارج مجلد الصور (اجتياز بـ .. أو مسار مطلق) — نتجاهل بدل الحذف.
        if (!fullPath.StartsWith(imagesRootPrefix, StringComparison.OrdinalIgnoreCase))
            return;

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    /// <summary>
    /// مسار wwwroot الموحّد لعمليتي الحفظ والحذف.
    /// </summary>
    private string ResolveWebRootPath()
        => string.IsNullOrEmpty(_env.WebRootPath)
            ? Path.Combine(_env.ContentRootPath, "wwwroot")
            : _env.WebRootPath;
}