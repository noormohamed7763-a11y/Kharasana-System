using Kharasana.Application.DTOs.Report;

namespace Kharasana.Web.ViewModels.Reports;

public class ReportsViewModel
{
    public int TotalOrders { get; set; }

    public List<OrderStatusCountDto> OrdersByStatus { get; set; } = new();

    public List<ConcreteTypeCountDto> OrdersByConcreteType { get; set; } = new();

    /// <summary>هل توجد بيانات فعلية لعرضها؟ (تفادي الرسوم الفارغة)</summary>
    public bool HasData => TotalOrders > 0;

    // اختصارات ملائمة للعرض

    public OrderStatusCountDto? NewOrders => OrdersByStatus.FirstOrDefault(s => s.Status == 0);
    public OrderStatusCountDto? PendingOrders => OrdersByStatus.FirstOrDefault(s => s.Status == 1);
    public OrderStatusCountDto? ApprovedOrders => OrdersByStatus.FirstOrDefault(s => s.Status == 2);
    public OrderStatusCountDto? RejectedOrders => OrdersByStatus.FirstOrDefault(s => s.Status == 3);
    public OrderStatusCountDto? CancelledOrders => OrdersByStatus.FirstOrDefault(s => s.Status == 4);
    public OrderStatusCountDto? OnTheWayOrders => OrdersByStatus.FirstOrDefault(s => s.Status == 5);
    public OrderStatusCountDto? DeliveredOrders => OrdersByStatus.FirstOrDefault(s => s.Status == 6);
    public OrderStatusCountDto? ClosedOrders => OrdersByStatus.FirstOrDefault(s => s.Status == 7);
}