using Kharasana.Domain.Enums;
using System;

namespace Kharasana.Web.Helpers
{
    /// <summary>
    /// طبقة العرض الوحيدة لحالة الطلب: الاسم العربي المزيَّن بالإيموجي، ولاحقة كلاس CSS،
    /// ونِسَب شريط التقدّم وألوانه.
    ///
    /// <para>هذا الملف هو <b>المكان الوحيد</b> المسموح فيه بكتابة خريطة (switch) لكل حالة.
    /// الاسم المجرّد مصدره <see cref="OrderStatusHelper.GetArabicName"/> في Domain،
    /// وقواعد الانتقال مصدرها <see cref="OrderStatusHelper.GetAllowedTransitions"/>.
    /// أي خريطة ثالثة في الويب (في View أو DTO أو خدمة) يرفضها اختبار
    /// <c>OrderStatusMapTests</c> — كان في المشروع ثلاث خرائط متوازية للأسماء
    /// (هنا، وفي <c>OrderDto.StatusArabic</c>) وثلاث للأصناف
    /// (هنا، وفي <c>DashboardApiService</c>، وفي <c>Reports/Index.cshtml</c>)،
    /// ثم خريطتان لنِسَب التقدّم وألوانه في <c>Views/Orders/Details.cshtml</c>.</para>
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

        /// <summary>
        /// نسبة شريط التقدّم المعروضة في صفحة تفاصيل الطلب.
        /// <b>ملاحظة:</b> هذه نسب عرض تخشينية لا مشتقّة من قواعد الانتقال —
        /// كانت خريطة محلية في <c>Views/Orders/Details.cshtml</c> ونُقلت هنا
        /// لتكون مع بقية خرائط الحالة في مكان واحد يحرسه <c>OrderStatusMapTests</c>.
        /// </summary>
        public static int GetProgressPercent(this OrderStatus status)
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
        /// صنف خلفية Bootstrap لشريط التقدّم — المقابل اللوني لـ <see cref="GetProgressPercent"/>.
        /// </summary>
        public static string GetProgressBgClass(this OrderStatus status)
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
        /// أصناف التخطيط والحركة لشريط التقدّم: تُمنح للحالات الجارية فقط،
        /// وترجع نصاً فارغاً لغيرها.
        ///
        /// <para>الشريط المتحرك المخطَّط يقول «يجري الآن» — وهذا كذب لطلب مسلَّم أو
        /// مغلق أو مرفوض أو ملغي، ويجعل صفحة التفاصيل تناقض نفسها: الشريط الزمني
        /// يعرض مراحله الأربع فارغة (○) لطلب مرفوض بينما الشريط ينبض.
        /// القاعدة نفسها («هل هناك عمل جارٍ؟») مصدرها
        /// <see cref="OrderStatusHelper.IsInFlight"/> في Domain.</para>
        /// </summary>
        public static string GetProgressMotionClasses(this OrderStatus status) =>
            OrderStatusHelper.IsInFlight(status)
                ? "progress-bar-striped progress-bar-animated"
                : string.Empty;
    }
}
