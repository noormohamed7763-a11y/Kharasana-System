namespace Kharasana.Domain.Enums;

/// <summary>
/// قواعد الحالة الموحّدة — المصدر الوحيد للأسماء العربية وجدول الانتقالات المسموحة.
/// طبقات Application (OrderService, OrderRepository) و Web (OrderStatusExtensions) تكلّف بهذا الصنف
/// بدلاً من تكرار الأسماء أو قواعد الانتقال في كل مكان.
/// </summary>
public static class OrderStatusHelper
{
    /// <summary>الاسم العربي للحالة بدون أي تنسيقات عرض (إيموجي/أيقونات).</summary>
    public static string GetArabicName(OrderStatus status) => status switch
    {
        OrderStatus.New => "جديد",
        OrderStatus.Pending => "قيد الانتظار",
        OrderStatus.Approved => "معتمد",
        OrderStatus.Rejected => "مرفوض",
        OrderStatus.Cancelled => "ملغي",
        OrderStatus.OnTheWay => "في الطريق",
        OrderStatus.Delivered => "تم التسليم",
        OrderStatus.Closed => "مغلق",
        _ => "غير معروف"
    };

    /// <summary>
    /// الحالات المسموح الانتقال إليها من الحالة الحالية.
    /// ملاحظة: السماح بالإلغاء ممتد حتى حالة "في الطريق" — مطابق لمنطق CancelOrderAsync
    /// وواجهة الويب (Approved → Cancelled و OnTheWay → Cancelled مسموحان).
    /// </summary>
    public static OrderStatus[] GetAllowedTransitions(OrderStatus status) => status switch
    {
        OrderStatus.New => new[] { OrderStatus.Pending, OrderStatus.Rejected, OrderStatus.Cancelled },
        OrderStatus.Pending => new[] { OrderStatus.Approved, OrderStatus.Rejected, OrderStatus.Cancelled },
        OrderStatus.Approved => new[] { OrderStatus.OnTheWay, OrderStatus.Cancelled },
        OrderStatus.OnTheWay => new[] { OrderStatus.Delivered, OrderStatus.Cancelled },
        OrderStatus.Delivered => new[] { OrderStatus.Closed },
        OrderStatus.Rejected => Array.Empty<OrderStatus>(),
        OrderStatus.Cancelled => Array.Empty<OrderStatus>(),
        OrderStatus.Closed => Array.Empty<OrderStatus>(),
        _ => Array.Empty<OrderStatus>()
    };

    /// <summary>هل الانتقال من الحالة الحالية إلى الحالة الجديدة مسموح؟</summary>
    public static bool CanTransitionTo(OrderStatus current, OrderStatus next)
        => GetAllowedTransitions(current).Contains(next);
}