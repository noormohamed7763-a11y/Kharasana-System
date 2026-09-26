using Kharasana.Domain.Enums;
using System;

namespace Kharasana.Web.Helpers
{
    /// <summary>
    /// طبقة العرض الوحيدة لحالة الطلب: الاسم العربي المزيَّن بالإيموجي، ولاحقة كلاس CSS.
    ///
    /// <para>هذا الملف هو <b>المكان الوحيد</b> المسموح فيه بكتابة خريطة (switch) تُرجع نصاً
    /// لكل حالة. الاسم المجرّد مصدره <see cref="OrderStatusHelper.GetArabicName"/> في Domain،
    /// وقواعد الانتقال مصدرها <see cref="OrderStatusHelper.GetAllowedTransitions"/>.
    /// أي خريطة ثالثة في الويب (في View أو DTO أو خدمة) يرفضها اختبار
    /// <c>OrderStatusMapTests</c> — كان في المشروع ثلاث خرائط متوازية للأسماء
    /// (هنا، وفي <c>OrderDto.StatusArabic</c>) وثلاث للأصناف
    /// (هنا، وفي <c>DashboardApiService</c>، وفي <c>Reports/Index.cshtml</c>).</para>
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
        /// لاحقة كلاس CSS للحالة بدون البادئة <c>status-</c>.
        /// الأنماط معرّفة في <c>wwwroot/css/components.css</c> بأحرف صغيرة.
        /// </summary>
        public static string GetCssSuffix(this OrderStatus status)
        {
            return status switch
            {
                // مهم: CSS حساس لحالة الأحرف — اللواحق كلها lowercase وتطابق components.css
                OrderStatus.New => "new",
                OrderStatus.Pending => "pending",
                OrderStatus.Approved => "approved",
                OrderStatus.Rejected => "rejected",
                OrderStatus.Cancelled => "cancelled",
                OrderStatus.OnTheWay => "ontheway",
                OrderStatus.Delivered => "delivered",
                OrderStatus.Closed => "closed",
                _ => "default"
            };
        }

        /// <summary>
        /// الحصول على كلاس CSS المناسب للحالة
        /// </summary>
        public static string GetCssClass(this OrderStatus status) => $"status-{status.GetCssSuffix()}";
    }
}
