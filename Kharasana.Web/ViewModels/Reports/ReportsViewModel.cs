using Kharasana.Application.DTOs.Report;
using Kharasana.Domain.Enums;

namespace Kharasana.Web.ViewModels.Reports;

public class ReportsViewModel
{
    public int TotalOrders { get; set; }

    public List<OrderStatusCountDto> OrdersByStatus { get; set; } = new();

    public List<ConcreteTypeCountDto> OrdersByConcreteType { get; set; } = new();

    /// <summary>هل توجد بيانات فعلية لعرضها؟ (تفادي الرسوم الفارغة)</summary>
    public bool HasData => TotalOrders > 0;

    // اختصارات ملائمة للعرض
    // ⚠️ لا تُضِف اختصاراً لـ OrderStatus.New: حالة لا تُسنَد لأي طلب في النظام
    //    (الطلبات تُنشأ Pending)، فأي مقياس مبني عليها يعرض صفراً دائماً.

    public OrderStatusCountDto? PendingOrders => OrdersByStatus.FirstOrDefault(s => s.Status == OrderStatus.Pending);
    public OrderStatusCountDto? ApprovedOrders => OrdersByStatus.FirstOrDefault(s => s.Status == OrderStatus.Approved);
    public OrderStatusCountDto? RejectedOrders => OrdersByStatus.FirstOrDefault(s => s.Status == OrderStatus.Rejected);
    public OrderStatusCountDto? CancelledOrders => OrdersByStatus.FirstOrDefault(s => s.Status == OrderStatus.Cancelled);
    public OrderStatusCountDto? OnTheWayOrders => OrdersByStatus.FirstOrDefault(s => s.Status == OrderStatus.OnTheWay);
    public OrderStatusCountDto? DeliveredOrders => OrdersByStatus.FirstOrDefault(s => s.Status == OrderStatus.Delivered);
    public OrderStatusCountDto? ClosedOrders => OrdersByStatus.FirstOrDefault(s => s.Status == OrderStatus.Closed);
}