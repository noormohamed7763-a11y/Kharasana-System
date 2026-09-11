using Kharasana.Domain.Enums;
using System;

namespace Kharasana.Web.Helpers
{
    /// <summary>
    /// دوال مساعدة لـ OrderStatus Enum
    /// توفر ترجمة وتنسيقات للحالة باللغة العربية
    /// </summary>
    public static class OrderStatusExtensions
    {
        /// <summary>
        /// الحصول على اسم الحالة باللغة العربية
        /// </summary>
        public static string GetArabicName(this OrderStatus status)
        {
            return status switch
            {
                OrderStatus.New => "جديد",
                OrderStatus.Pending => "⏳ قيد الانتظار",
                OrderStatus.Approved => "✅ معتمد",
                OrderStatus.Rejected => "❌ مرفوض",
                OrderStatus.Cancelled => "🚫 ملغي",
                OrderStatus.OnTheWay => "🚚 في الطريق",
                OrderStatus.Delivered => "📦 تم التسليم",
                OrderStatus.Closed => "🔒 مغلق",
                _ => "غير معروف"
            };
        }

        /// <summary>
        /// الحصول على كلاس CSS المناسب للحالة
        /// </summary>
        public static string GetCssClass(this OrderStatus status)
        {
            return status switch
            {
                OrderStatus.New => "status-New",
                OrderStatus.Pending => "status-Pending",
                OrderStatus.Approved => "status-Approved",
                OrderStatus.Rejected => "status-Rejected",
                OrderStatus.Cancelled => "status-Cancelled",
                OrderStatus.OnTheWay => "status-OnTheWay",
                OrderStatus.Delivered => "status-Delivered",
                OrderStatus.Closed => "status-Closed",
                _ => "status-default"
            };
        }

        /// <summary>
        /// الحصول على نسبة التقدم للحالة (0-100)
        /// </summary>
        public static int GetProgressPercentage(this OrderStatus status)
        {
            return status switch
            {
                OrderStatus.New => 10,
                OrderStatus.Pending => 25,
                OrderStatus.Approved => 50,
                OrderStatus.Rejected => 30,
                OrderStatus.Cancelled => 20,
                OrderStatus.OnTheWay => 65,
                OrderStatus.Delivered => 85,
                OrderStatus.Closed => 100,
                _ => 10
            };
        }

        /// <summary>
        /// الحصول على لون شريط التقدم المناسب للحالة
        /// </summary>
        public static string GetProgressColor(this OrderStatus status)
        {
            return status switch
            {
                OrderStatus.New => "bg-info",
                OrderStatus.Pending => "bg-warning",
                OrderStatus.Approved => "bg-primary",
                OrderStatus.Rejected => "bg-danger",
                OrderStatus.Cancelled => "bg-secondary",
                OrderStatus.OnTheWay => "bg-warning",
                OrderStatus.Delivered => "bg-success",
                OrderStatus.Closed => "bg-secondary",
                _ => "bg-info"
            };
        }

        /// <summary>
        /// التحقق مما إذا كانت الحالة تسمح بالتعديل
        /// </summary>
        public static bool CanEdit(this OrderStatus status)
        {
            return status == OrderStatus.New || status == OrderStatus.Pending;
        }

        /// <summary>
        /// التحقق مما إذا كانت الحالة تسمح بالحذف
        /// </summary>
        public static bool CanDelete(this OrderStatus status)
        {
            return status == OrderStatus.New || status == OrderStatus.Pending;
        }

        /// <summary>
        /// التحقق مما إذا كانت الحالة نهائية (لا يمكن تغييرها)
        /// </summary>
        public static bool IsFinal(this OrderStatus status)
        {
            return status == OrderStatus.Rejected ||
                   status == OrderStatus.Cancelled ||
                   status == OrderStatus.Closed;
        }

        /// <summary>
        /// التحقق مما إذا كانت الحالة تتطلب تعيين سائق
        /// </summary>
        public static bool NeedsDriver(this OrderStatus status)
        {
            return status == OrderStatus.Approved;
        }

        /// <summary>
        /// الحصول على وصف مختصر للحالة
        /// </summary>
        public static string GetDescription(this OrderStatus status)
        {
            return status switch
            {
                OrderStatus.New => "طلب جديد لم يتم معالجته بعد",
                OrderStatus.Pending => "في انتظار موافقة العميل أو التسعير",
                OrderStatus.Approved => "تمت موافقة العميل وجاهز للتوصيل",
                OrderStatus.Rejected => "تم رفض الطلب من قبل العميل أو الإدارة",
                OrderStatus.Cancelled => "تم إلغاء الطلب",
                OrderStatus.OnTheWay => "في طريقه إلى موقع العميل",
                OrderStatus.Delivered => "تم تسليم الطلب للعميل",
                OrderStatus.Closed => "تم إغلاق الطلب وإكمال جميع الإجراءات",
                _ => "حالة غير معروفة"
            };
        }

        /// <summary>
        /// الحصول على الخطوة التالية في دورة حياة الطلب
        /// </summary>
        public static OrderStatus? GetNextStatus(this OrderStatus status)
        {
            return status switch
            {
                OrderStatus.New => OrderStatus.Pending,
                OrderStatus.Pending => OrderStatus.Approved,
                OrderStatus.Approved => OrderStatus.OnTheWay,
                OrderStatus.OnTheWay => OrderStatus.Delivered,
                OrderStatus.Delivered => OrderStatus.Closed,
                OrderStatus.Rejected => null,
                OrderStatus.Cancelled => null,
                OrderStatus.Closed => null,
                _ => null
            };
        }

        /// <summary>
        /// الحصول على قائمة بالحالات المسموح بالانتقال إليها من الحالة الحالية
        /// </summary>
        public static OrderStatus[] GetAllowedTransitions(this OrderStatus status)
        {
            return status switch
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
        }

        /// <summary>
        /// الحصول على أيقونة Bootstrap المناسبة للحالة
        /// </summary>
        public static string GetIcon(this OrderStatus status)
        {
            return status switch
            {
                OrderStatus.New => "bi-plus-circle",
                OrderStatus.Pending => "bi-hourglass-split",
                OrderStatus.Approved => "bi-check-circle",
                OrderStatus.Rejected => "bi-x-circle",
                OrderStatus.Cancelled => "bi-ban",
                OrderStatus.OnTheWay => "bi-truck",
                OrderStatus.Delivered => "bi-box-seam",
                OrderStatus.Closed => "bi-lock",
                _ => "bi-question-circle"
            };
        }

        /// <summary>
        /// الحصول على لون Bootstrap المناسب للحالة
        /// </summary>
        public static string GetBootstrapColor(this OrderStatus status)
        {
            return status switch
            {
                OrderStatus.New => "primary",
                OrderStatus.Pending => "warning",
                OrderStatus.Approved => "success",
                OrderStatus.Rejected => "danger",
                OrderStatus.Cancelled => "secondary",
                OrderStatus.OnTheWay => "warning",
                OrderStatus.Delivered => "success",
                OrderStatus.Closed => "secondary",
                _ => "secondary"
            };
        }
    }
}