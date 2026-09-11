namespace Kharasana.Application.Interfaces.Services;

/// <summary>
/// خدمة عامة لإدارة الملفات (حفظ وحذف) بغض النظر عن مكان التخزين الفعلي
/// (Local Disk اليوم، ويمكن استبدالها لاحقاً بـ Azure/S3 دون تعديل أي كود آخر).
/// </summary>
public interface IImageStorageService
{
    /// <summary>
    /// يحفظ صورة داخل مجلد فرعي معين تحت wwwroot/Images ويرجع المسار النسبي للصورة.
    /// </summary>
    /// <param name="fileStream">محتوى الملف</param>
    /// <param name="originalFileName">اسم الملف الأصلي (لاستخراج الامتداد)</param>
    /// <param name="fileLength">حجم الملف بالبايت</param>
    /// <param name="folderName">اسم المجلد الفرعي (مثال: "Factories")</param>
    Task<string> SaveImageAsync(Stream fileStream, string originalFileName, long fileLength, string folderName);

    /// <summary>
    /// يحذف صورة موجودة بناءً على مسارها النسبي (مثال: "/Images/Factories/abc.png").
    /// لا يفعل شيئاً إذا كان المسار فارغاً أو الملف غير موجود.
    /// </summary>
    void DeleteImage(string? relativePath);
}