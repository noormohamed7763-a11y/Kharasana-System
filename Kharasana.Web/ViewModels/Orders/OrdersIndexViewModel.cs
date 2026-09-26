using Kharasana.Application.Common;
using Kharasana.Web.ViewModels.Orders;

namespace Kharasana.Web.ViewModels.Orders
{
    public class OrdersIndexViewModel
    {
        public PagedResult<OrderDto>? PagedOrders { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public int? StatusFilter { get; set; }
        public int? FactoryIdFilter { get; set; }

        // ✅ عدّادات البطاقات — إجماليات حقيقية عبر كل الصفحات
        //    (بطاقات "مكتملة" و"ملغاة/مرفوضة" تجمع أكثر من حالة في العرض)
        public int PendingCount { get; set; }
        public int RejectedCount { get; set; }
        public int CancelledCount { get; set; }
        public int ClosedCount { get; set; }
    }
}