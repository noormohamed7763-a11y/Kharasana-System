using System.ComponentModel.DataAnnotations;
using Kharasana.Domain.Enums;

namespace Kharasana.Web.ViewModels.Orders
{
    public class EditOrderViewModel
    {
        public int OrderId { get; set; }

        [Required(ErrorMessage = "نوع الخرسانة مطلوب")]
        public int ConcreteTypeId { get; set; }

        // ✅ سقوف النصوص الحرة = أطوال الأعمدة في قاعدة البيانات (OrderConfiguration).
        //    كانت غائبة هنا وفي UpdateOrderDtoValidator معاً، فحقل أطول من العمود
        //    يصل إلى SQL Server فيرمي 8152 → صفحة 500 ويُفقد ما كتبه المستخدم.
        //    هذه الرسائل هي ما يراه المستخدم على النموذج قبل النداء.
        [StringLength(200, ErrorMessage = "اسم المشروع لا يزيد عن 200 حرف")]
        public string? ProjectName { get; set; }

        [StringLength(200, ErrorMessage = "اسم المالك لا يزيد عن 200 حرف")]
        public string? ProjectOwnerName { get; set; }

        [StringLength(100, ErrorMessage = "المنطقة لا تزيد عن 100 حرف")]
        public string? SiteArea { get; set; }

        [StringLength(500, ErrorMessage = "وصف الموقع لا يزيد عن 500 حرف")]
        public string? SiteDescription { get; set; }

        [Required(ErrorMessage = "نوع البلاطة مطلوب")]
        public SlabType SlabType { get; set; }

        [Required(ErrorMessage = "الكمية مطلوبة")]
        // ✅ السقف 1000 لا 100000 — نفس سبب CreatePhoneOrderViewModel:
        //    مطابقةُ سقف ValidationRules.Quantity() الذي يرفض ما فوق 1000.
        [Range(typeof(decimal), "0.1", "1000", ErrorMessage = "الكمية يجب أن تكون بين 0.1 و 1000 م³")]
        public decimal Quantity { get; set; }

        public bool NeedPump { get; set; }
        public int? FloorNumber { get; set; }

        [Required(ErrorMessage = "تاريخ الصب مطلوب")]
        public DateTime? PouringDate { get; set; }

        [Required(ErrorMessage = "طريقة النقل مطلوبة")]
        public TransportMethod TransportMethod { get; set; }

        [StringLength(1000, ErrorMessage = "الملاحظات لا تزيد عن 1000 حرف")]
        public string? Notes { get; set; }
    }
}