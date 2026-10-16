using Kharasana.Domain.Enums;

namespace Kharasana.Application.Common;

/// <summary>
/// معاملات الترقيم والبحث والفلترة الخاصة بطلبات تسجيل المصانع.
/// مبنية على نفس نمط <see cref="PaginationParams"/> للاتساق،
/// لكن بـ <see cref="RegistrationStatus"/> بدلاً من <see cref="OrderStatus"/>
/// لأنها تنتمي إلى دورة حياة مختلفة تماماً.
/// </summary>
public class RegistrationRequestFilterParams
{
    private const int MaxPageSize = 100;
    private int _pageNumber = 1;
    private int _pageSize = 10;

    /// <summary>رقم الصفحة — يُقنَّن إلى 1 كحد أدنى.</summary>
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    /// <summary>حجم الصفحة — يُقنَّن بين 1 و100.</summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 1 : (value > MaxPageSize ? MaxPageSize : value);
    }

    /// <summary>بحث جزئي (اسم المصنع / اسم المالك / البريد / المنطقة).</summary>
    public string? Search { get; set; }

    /// <summary>
    /// فلتر حالة الطلب. إذا كان null فتُعاد كل الحالات.
    /// </summary>
    public RegistrationStatus? Status { get; set; }
}
