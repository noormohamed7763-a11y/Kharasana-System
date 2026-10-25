using Kharasana.Domain.Enums;

namespace Kharasana.Application.DTOs.Order
{
    public class OrderPaymentDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }

        // استخدام خاصية للقراءة فقط لعرض النص،
        // سيقوم AutoMapper (إن وجد) بتجاهلها أو التعامل معها
        public string PaymentMethodDisplay => PaymentMethod.ToString();

        public string? ReferenceNo { get; set; }
        public string? Notes { get; set; }

        // حقول التحقق من الحوالات
        public PaymentVerificationStatus VerificationStatus { get; set; }
        public string? VerificationNotes { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }
}
