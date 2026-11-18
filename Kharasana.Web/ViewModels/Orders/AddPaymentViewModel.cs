using System.ComponentModel.DataAnnotations;
using Kharasana.Domain.Enums;

namespace Kharasana.Web.ViewModels.Orders;

public class AddPaymentViewModel
{
    [Required(ErrorMessage = "المبلغ مطلوب.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "المبلغ يجب أن يكون أكبر من صفر.")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "طريقة الدفع مطلوبة.")]
    public PaymentMethod PaymentMethod { get; set; }

    public string? ReferenceNo { get; set; }

    public string? Notes { get; set; }
}
