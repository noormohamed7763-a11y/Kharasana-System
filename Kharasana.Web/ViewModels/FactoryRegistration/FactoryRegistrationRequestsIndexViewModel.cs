using Kharasana.Application.Common;

namespace Kharasana.Web.ViewModels.FactoryRegistration;

public class FactoryRegistrationRequestsIndexViewModel
{
    /// <summary>بيانات الصفحة الحالية مع إجمالي العدد للترقيم.</summary>
    public PagedResult<RegistrationRequestListItemViewModel>? PagedRequests { get; set; }

    /// <summary>
    /// معاملات الفلترة الحالية — تُمرَّر للـ View لبناء نموذج البحث والترقيم.
    /// </summary>
    public RegistrationRequestFilterParams Filter { get; set; } = new();

    /// <summary>إجمالي جميع الطلبات بمختلف حالاتها.</summary>
    public int TotalAllRequestsCount { get; set; }

    /// <summary>عدد الطلبات قيد الانتظار (تحتاج مراجعة).</summary>
    public int PendingCount { get; set; }

    /// <summary>عدد الطلبات المقبولة.</summary>
    public int ApprovedCount { get; set; }

    /// <summary>عدد الطلبات المرفوضة.</summary>
    public int RejectedCount { get; set; }
}