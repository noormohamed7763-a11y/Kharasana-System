using Kharasana.Domain.Enums;
using System;

namespace Kharasana.Web.Helpers
{
    /// <summary>
    /// دوال مساعدة للعرض لـ OrderStatus Enum
    /// (قواعد الانتقال بين الحالات مصدرها الوحيد OrderStatusHelper في Domain)
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
    }
}
