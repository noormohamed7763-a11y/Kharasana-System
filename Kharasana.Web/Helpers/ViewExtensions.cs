using Kharasana.Domain.Enums;
using System;

namespace Kharasana.Web.Helpers
{
    /// <summary>
    /// دوال مساعدة إضافية للـ Views
    /// </summary>
    public static class ViewExtensions
    {
        /// <summary>
        /// تنسيق السعر مع العملة
        /// </summary>
        public static string FormatPrice(this decimal? price)
        {
            if (!price.HasValue || price.Value <= 0)
                return "-";
            return $"{price.Value:N0} ر.ي";
        }

        /// <summary>
        /// تنسيق السعر مع العملة
        /// </summary>
        public static string FormatPrice(this decimal price)
        {
            if (price <= 0)
                return "-";
            return $"{price:N0} ر.ي";
        }

        /// <summary>
        /// تنسيق التاريخ
        /// </summary>
        public static string FormatDate(this DateTime? date)
        {
            if (!date.HasValue)
                return "-";
            return date.Value.ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// تنسيق التاريخ مع الوقت
        /// </summary>
        public static string FormatDateTime(this DateTime? date)
        {
            if (!date.HasValue)
                return "-";
            return date.Value.ToString("yyyy-MM-dd HH:mm");
        }
    }
}