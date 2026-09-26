using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Kharasana.Domain.Enums;

namespace Kharasana.Web.ViewModels.Orders
{
    /// <summary>
    /// نموذج تحديث حالة الطلب
    /// يستخدم عند تغيير حالة الطلب من قبل موظف المصنع أو المدير
    /// </summary>
    public class UpdateOrderStatusViewModel
    {
        // ============================================================
        // 1. STATUS - الحالة الجديدة
        // ============================================================

        /// <summary>
        /// الحالة الجديدة للطلب (قيمة من enum OrderStatus)
        /// يُرسل إلى الـ API كقيمة بترميز string أو عددي بحسب إعدادات JsonStringEnumConverter
        /// </summary>
         [Required(ErrorMessage = "الحالة مطلوبة")]
        [EnumDataType(typeof(OrderStatus), ErrorMessage = "الحالة غير صالحة")]
        [Display(Name = "الحالة")]
        public OrderStatus Status { get; set; }

        // ============================================================
        // 2. OPTIONAL FIELDS - حقول اختيارية
        // ============================================================

        /// <summary>
        /// سبب الرفض (يُطلب فقط عند تغيير الحالة إلى Rejected)
        /// </summary>
        [Display(Name = "سبب الرفض")]
        [StringLength(500, ErrorMessage = "سبب الرفض لا يزيد عن 500 حرف")]
        public string? RejectionReason { get; set; }

        /// <summary>
        /// ملاحظات إضافية عن تغيير الحالة
        /// </summary>
        [Display(Name = "ملاحظات")]
        [StringLength(500, ErrorMessage = "الملاحظات لا تزيد عن 500 حرف")]
        public string? Notes { get; set; }

        // ============================================================
        // 3. COMPUTED PROPERTIES - خصائص محسوبة (تُستخدم في الواجهة فقط)
        //    [JsonIgnore] يمنع إرسالها داخل جسم طلب الـ API
        // ============================================================

        /// <summary>
        /// هل الحالة المطلوبة هي "مرفوض"؟
        /// </summary>
        [JsonIgnore]
        public bool IsRejected => Status == OrderStatus.Rejected;

        /// <summary>
        /// هل الحالة المطلوبة هي "مغلق"؟
        /// </summary>
        [JsonIgnore]
        public bool IsClosed => Status == OrderStatus.Closed;

        /// <summary>
        /// هل الحالة المطلوبة هي "تم التسليم"؟
        /// </summary>
        [JsonIgnore]
        public bool IsDelivered => Status == OrderStatus.Delivered;

        /// <summary>
        /// هل الحالة المطلوبة هي "معتمد"؟
        /// </summary>
        [JsonIgnore]
        public bool IsApproved => Status == OrderStatus.Approved;

        /// <summary>
        /// هل الحالة المطلوبة هي "في الطريق"؟
        /// </summary>
        [JsonIgnore]
        public bool IsOnTheWay => Status == OrderStatus.OnTheWay;

        /// <summary>
        /// هل الحالة المطلوبة هي "قيد الانتظار"؟
        /// </summary>
        [JsonIgnore]
        public bool IsPending => Status == OrderStatus.Pending;

        // ============================================================
        // 4. VALIDATION
        // ============================================================

        /// <summary>
        /// التحقق من صحة النموذج (خاصة سبب الرفض)
        /// </summary>
        public bool IsValid()
        {
            if (IsRejected && string.IsNullOrWhiteSpace(RejectionReason))
                return false;
            return true;
        }
    }
}