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
        /// الحصول على اسم الحالة باللغة العربية مع إيموجي توضيحي
        /// (الاسم النصي مصدره الوحيد هو OrderStatusHelper في Domain — هنا إضافة إيموجي العرض فقط)
        /// </summary>
        public static string GetArabicName(this OrderStatus status)
        {
            string emoji = status switch
            {
                OrderStatus.Pending => "⏳",
                OrderStatus.Approved => "✅",
                OrderStatus.Rejected => "❌",
                OrderStatus.Cancelled => "🚫",
                OrderStatus.OnTheWay => "🚚",
                OrderStatus.Delivered => "📦",
                OrderStatus.Closed => "🔒",
                _ => string.Empty
            };

            var name = OrderStatusHelper.GetArabicName(status);
            return string.IsNullOrEmpty(emoji) ? name : $"{emoji} {name}";
        }

        /// <summary>
        /// الحصول على كلاس CSS المناسب للحالة
        /// </summary>
        public static string GetCssClass(this OrderStatus status)
        {
            return status switch
            {
                // مهم: CSS حساس لحالة الأحرف — الأصناف كلها lowercase وتطابق components.css
                OrderStatus.New => "status-new",
                OrderStatus.Pending => "status-pending",
                OrderStatus.Approved => "status-approved",
                OrderStatus.Rejected => "status-rejected",
                OrderStatus.Cancelled => "status-cancelled",
                OrderStatus.OnTheWay => "status-ontheway",
                OrderStatus.Delivered => "status-delivered",
                OrderStatus.Closed => "status-closed",
                _ => "status-default"
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
        /// (المصدر الوحيد هو OrderStatusHelper في Domain)
        /// </summary>
        public static OrderStatus[] GetAllowedTransitions(this OrderStatus status)
        {
            return OrderStatusHelper.GetAllowedTransitions(status);
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