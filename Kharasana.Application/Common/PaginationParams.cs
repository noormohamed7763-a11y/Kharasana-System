namespace Kharasana.Application.Common;

using Kharasana.Domain.Enums;

public class PaginationParams
{
    private const int MaxPageSize = 100;
    private int _pageNumber = 1;
    private int _pageSize = 20;

    /// <summary>رقم الصفحة — يُقنَّن إلى 1 كحد أدنى حتى لا يمرّر قيمة سالبة إلى Skip.</summary>
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 1 : (value > MaxPageSize ? MaxPageSize : value);
    }

    // بحث جزئي عام (بالاسم/الهاتف/البريد/رقم الطلب حسب السياق)
    public string? Search { get; set; }

    /// <summary>فلتر حالة الطلبات (يُستخدم في قائمة الطلبات).</summary>
    public OrderStatus? Status { get; set; }

    /// <summary>فلتر المصنع (يستخدمه المدير لتضييق نطاق القائمة).</summary>
    public int? FactoryId { get; set; }
}