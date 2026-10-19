using Kharasana.Domain.Enums;

namespace Kharasana.Application.DTOs.Order;

public class AddPaymentDto
{
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Notes { get; set; }
}
