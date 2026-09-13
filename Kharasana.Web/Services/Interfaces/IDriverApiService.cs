using Kharasana.Application.Common;
using Kharasana.Web.ViewModels.Drivers;

namespace Kharasana.Web.Services.Interfaces;

/// <summary>
/// واجهة خدمات السائقين - مسؤولة عن التواصل مع الـ API الخاص بالسائقين
/// </summary>
public interface IDriverApiService
{
    /// <summary>
    /// جلب قائمة السائقين مع ترقيم الصفحات والبحث
    /// </summary>
    /// <param name="pageNumber">رقم الصفحة (تبدأ من 1)</param>
    /// <param name="pageSize">عدد العناصر في الصفحة (الحد الأقصى 100)</param>
    /// <param name="search">نص البحث (اختياري) - يبحث في الاسم ورقم الهاتف</param>
    /// <param name="factoryId">معرف المصنع (اختياري) - لعزل البيانات حسب المصنع</param>
    /// <param name="cancellationToken">رمز إلغاء الطلب (اختياري)</param>
    /// <returns>قائمة السائقين مع معلومات الترقيم، أو null في حالة الفشل</returns>
    Task<PagedResult<DriverListItemViewModel>?> GetDriversAsync(
        int pageNumber = 1,
        int pageSize = 20,
        string? search = null,
        int? factoryId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// جلب بيانات سائق محدد بواسطة المعرف
    /// </summary>
    /// <param name="id">معرف السائق</param>
    /// <param name="cancellationToken">رمز إلغاء الطلب (اختياري)</param>
    /// <returns>بيانات السائق، أو null إذا لم يتم العثور عليه</returns>
    Task<DriverListItemViewModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// جلب بيانات سائق للتعديل (صفحة Edit)
    /// </summary>
    /// <param name="driverId">معرف السائق</param>
    /// <param name="cancellationToken">رمز إلغاء الطلب (اختياري)</param>
    /// <returns>بيانات السائق المعدة للتعديل، أو null إذا لم يتم العثور عليه</returns>
    Task<EditDriverViewModel?> GetForEditAsync(int driverId, CancellationToken cancellationToken = default);

    /// <summary>
    /// إنشاء سائق جديد
    /// </summary>
    /// <param name="model">بيانات السائق الجديد</param>
    /// <param name="cancellationToken">رمز إلغاء الطلب (اختياري)</param>
    /// <returns>tuple يحتوي على (نجاح العملية، رسالة الخطأ إن وجدت)</returns>
    Task<(bool Success, string? Message)> CreateAsync(CreateDriverViewModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// تحديث بيانات سائق موجود
    /// </summary>
    /// <param name="id">معرف السائق</param>
    /// <param name="model">بيانات السائق المحدثة</param>
    /// <param name="cancellationToken">رمز إلغاء الطلب (اختياري)</param>
    /// <returns>tuple يحتوي على (نجاح العملية، رسالة الخطأ إن وجدت)</returns>
    Task<(bool Success, string? Message)> UpdateAsync(int id, EditDriverViewModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// حذف/تعطيل سائق
    /// </summary>
    /// <param name="id">معرف السائق</param>
    /// <param name="cancellationToken">رمز إلغاء الطلب (اختياري)</param>
    /// <returns>true إذا تم الحذف بنجاح، false في حالة الفشل</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// تحديث حالة السائق (متاح، مشغول، غير متصل)
    /// </summary>
    /// <param name="id">معرف السائق</param>
    /// <param name="model">الحالة الجديدة</param>
    /// <param name="cancellationToken">رمز إلغاء الطلب (اختياري)</param>
    /// <returns>true إذا تم التحديث بنجاح، false في حالة الفشل</returns>
    Task<bool> UpdateStatusAsync(int id, UpdateDriverStatusViewModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// جلب أعداد السائقين حسب الحالة (متاح، مشغول، غير متصل) عبر كل الصفحات —
    /// لتغذية بطاقات الإحصاءات بالأرقام الحقيقية بدلاً من عدّ الصفحة الحالية فقط.
    /// </summary>
    /// <param name="search">نص البحث (اختياري) — يحسب النتائج ضمن نفس سياق البحث المعروض.</param>
    /// <param name="factoryId">معرف المصنع (اختياري) — لعزل البيانات حسب المصنع.</param>
    /// <param name="cancellationToken">رمز إلغاء الطلب (اختياري)</param>
    Task<(int Available, int Busy, int Offline)> GetStatusCountsAsync(
        string? search,
        int? factoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// تفعيل/إيقاف حساب السائق (عكس حالة IsActive)
    /// </summary>
    /// <param name="id">معرف السائق</param>
    /// <param name="cancellationToken">رمز إلغاء الطلب (اختياري)</param>
    /// <returns>true إذا أصبح السائق مفعلاً، false إذا أصبح معطلاً</returns>
    Task<bool> ToggleActiveAsync(int id, CancellationToken cancellationToken = default);
}