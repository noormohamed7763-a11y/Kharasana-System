namespace Kharasana.Application.DTOs.Dashboard;

public class FactoryDashboardDto
{
    /// <summary>
    /// عدد الطلبات المُنشأة اليوم لهذا المصنع.
    /// ملاحظة: كان الاسم السابق NewOrders ويُحتسب من OrderStatus.New — وهي حالة لا تُسنَد
    /// في أي مسار إنشاء (الطلبات تُنشأ Pending)، فكان الرقم صفرًا دائمًا. أُعيدت التسمية
    /// لتطابق ما تعرضه الواجهة فعلًا («طلبات اليوم»).
    /// </summary>
    public int TodayOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ApprovedOrders { get; set; }
    public int OnTheWayOrders { get; set; }
    public int DeliveredToday { get; set; }
    public int AvailableDrivers { get; set; }
}