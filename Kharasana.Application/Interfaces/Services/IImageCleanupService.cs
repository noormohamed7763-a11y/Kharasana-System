namespace Kharasana.Application.Interfaces.Services;

/// <summary>
/// خدمة تنظيف الملفات اليتيمة (Orphaned Files).
/// تقارن الملفات الموجودة على القرص بالمسارات المسجلة في قاعدة البيانات
/// وتحذف الملفات غير المرتبطة بأي سجل.
/// </summary>
public interface IImageCleanupService
{
    /// <summary>
    /// يفحص مجلدات الصور ويحذف الملفات اليتيمة.
    /// </summary>
    /// <returns>عدد الملفات التي تم حذفها.</returns>
    Task<int> CleanupOrphanedImagesAsync();

    /// <summary>
    /// يفحص مجلدات الصور ويعيد قائمة بالملفات اليتيمة دون حذفها.
    /// </summary>
    /// <returns>قائمة المسارات النسبية للملفات اليتيمة.</returns>
    Task<List<string>> FindOrphanedImagesAsync();
}
