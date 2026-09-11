using System;
using System.ComponentModel.DataAnnotations;
using Kharasana.Domain.Enums;

namespace Kharasana.Web.ViewModels.Orders
{
    public class EditOrderViewModel
    {
        public int OrderId { get; set; }  // ✅ أضيفت هذه الخاصية

        [Required(ErrorMessage = "نوع الخرسانة مطلوب")]
        public int ConcreteTypeId { get; set; }

        public string? ProjectName { get; set; }
        public string? ProjectOwnerName { get; set; }
        public string? SiteArea { get; set; }
        public string? SiteDescription { get; set; }

        [Required(ErrorMessage = "نوع البلاطة مطلوب")]
        public SlabType SlabType { get; set; }  // ✅ تغيير من int? إلى SlabType

        [Required(ErrorMessage = "الكمية مطلوبة")]
        [Range(0.1, double.MaxValue, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر")]
        public double Quantity { get; set; }

        public bool NeedPump { get; set; }
        public int? FloorNumber { get; set; }

        [Required(ErrorMessage = "تاريخ الصب مطلوب")]
        public DateTime? PouringDate { get; set; }

        [Required(ErrorMessage = "طريقة النقل مطلوبة")]
        public TransportMethod TransportMethod { get; set; }  // ✅ تغيير من int? إلى TransportMethod

        public string? Notes { get; set; }
    }
}