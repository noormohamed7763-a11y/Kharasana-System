using System.ComponentModel.DataAnnotations;
using Kharasana.Domain.Enums;

namespace Kharasana.Web.ViewModels.Orders
{
    /// <summary>
    /// نموذج إنشاء طلب هاتفي - يستخدمه موظف المصنع عند استقبال طلب عبر الهاتف
    /// </summary>
    public class CreatePhoneOrderViewModel
    {
        // ============================================================
        // 1. CLIENT INFO - معلومات العميل
        // ============================================================

        [Required(ErrorMessage = "رقم هاتف العميل مطلوب")]
        [Display(Name = "رقم هاتف العميل")]
        [RegularExpression(@"^[0-9]{7,15}$", ErrorMessage = "رقم الهاتف غير صالح")]
        public string ClientPhone { get; set; } = string.Empty;

        [Display(Name = "اسم العميل")]
        [StringLength(200, ErrorMessage = "الاسم لا يزيد عن 200 حرف")]
        public string? ClientFullName { get; set; }


        // ============================================================
        // 2. ORDER INFO - معلومات الطلب الأساسية
        // ============================================================

        [Required(ErrorMessage = "المصنع مطلوب")]
        [Display(Name = "المصنع")]
        public int FactoryId { get; set; }

        [Required(ErrorMessage = "نوع الخرسانة مطلوب")]
        [Display(Name = "نوع الخرسانة")]
        public int ConcreteTypeId { get; set; }


        // ============================================================
        // 3. CUSTOM CONCRETE - نوع الخرسانة المخصص (✅ جديد)
        // ============================================================

        /// <summary>
        /// هل المستخدم اختار "أخرى"؟
        /// </summary>
        [Display(Name = "نوع مخصص")]
        public bool IsCustomConcrete { get; set; }

        /// <summary>
        /// اسم النوع المخصص (يظهر فقط عند اختيار "أخرى")
        /// </summary>
        [Display(Name = "اسم النوع المخصص")]
        [StringLength(100, ErrorMessage = "اسم النوع لا يزيد عن 100 حرف")]
        public string? CustomConcreteName { get; set; }

        /// <summary>
        /// مقاومة النوع المخصص (يظهر فقط عند اختيار "أخرى")
        /// </summary>
        [Display(Name = "المقاومة (MPa)")]
        [Range(1, 100, ErrorMessage = "المقاومة بين 1 و 100 MPa")]
        public int? CustomConcreteStrength { get; set; }


        // ============================================================
        // 4. PROJECT INFO - معلومات المشروع
        // ============================================================

        [Display(Name = "اسم المشروع")]
        [StringLength(200, ErrorMessage = "اسم المشروع لا يزيد عن 200 حرف")]
        public string? ProjectName { get; set; }

        [Display(Name = "اسم مالك المشروع")]
        [StringLength(200, ErrorMessage = "اسم المالك لا يزيد عن 200 حرف")]
        public string? ProjectOwnerName { get; set; }

        [Display(Name = "المنطقة")]
        [StringLength(100, ErrorMessage = "المنطقة لا تزيد عن 100 حرف")]
        public string? SiteArea { get; set; }

        [Display(Name = "وصف الموقع")]
        [StringLength(500, ErrorMessage = "وصف الموقع لا يزيد عن 500 حرف")]
        public string? SiteDescription { get; set; }


        // ============================================================
        // 5. CONCRETE INFO - معلومات الخرسانة
        // ============================================================

        [Required(ErrorMessage = "نوع البلاطة مطلوب")]
        [Display(Name = "نوع البلاطة")]
        public SlabType SlabType { get; set; }

        [Required(ErrorMessage = "الكمية مطلوبة")]
        [Range(typeof(decimal), "0.1", "100000", ErrorMessage = "الكمية يجب أن تكون بين 0.1 و 100,000 م³")]
        [Display(Name = "الكمية (م³)")]
        public decimal Quantity { get; set; }


        // ============================================================
        // 6. TRANSPORT INFO - معلومات النقل
        // ============================================================

        [Display(Name = "بحاجة مضخة")]
        public bool NeedPump { get; set; }

        [Display(Name = "رقم الطابق")]
        [Range(0, 100, ErrorMessage = "رقم الطابق بين 0 و 100")]
        public int? FloorNumber { get; set; }

        [Required(ErrorMessage = "تاريخ الصب مطلوب")]
        [Display(Name = "تاريخ الصب")]
        [DataType(DataType.Date)]
        public DateTime? PouringDate { get; set; }

        [Required(ErrorMessage = "طريقة النقل مطلوبة")]
        [Display(Name = "طريقة النقل")]
        public TransportMethod TransportMethod { get; set; }


        // ============================================================
        // 7. NOTES - ملاحظات
        // ============================================================

        [Display(Name = "ملاحظات")]
        [StringLength(1000, ErrorMessage = "الملاحظات لا تزيد عن 1000 حرف")]
        public string? Notes { get; set; }


        // ============================================================
        // 8. HELPER PROPERTIES - خصائص مساعدة للـ View
        // ============================================================

        /// <summary>
        /// عرض اسم العميل - إذا كان فارغاً يعرض "عميل جديد"
        /// </summary>
        public string ClientDisplayName => string.IsNullOrWhiteSpace(ClientFullName) ? "عميل جديد" : ClientFullName;

        /// <summary>
        /// التحقق من صحة رقم الهاتف (7-15 رقم)
        /// </summary>
        public bool IsPhoneValid => !string.IsNullOrWhiteSpace(ClientPhone) &&
                                     System.Text.RegularExpressions.Regex.IsMatch(ClientPhone, @"^[0-9]{7,15}$");

        /// <summary>
        /// هل النوع المخصص مكتمل؟
        /// </summary>
        public bool IsCustomConcreteValid => IsCustomConcrete &&
                                             !string.IsNullOrWhiteSpace(CustomConcreteName) &&
                                             CustomConcreteStrength.HasValue &&
                                             CustomConcreteStrength.Value > 0;
    }
}