using Kharasana.Domain.Enums;

namespace Kharasana.Web.ViewModels.Orders;

public class PaymentReceiptViewModel
{
    public int PaymentId { get; set; }
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string PaymentMethodArabic => PaymentMethodHelper.GetArabicName(PaymentMethod);
    public string? ReferenceNo { get; set; }
    public string? Notes { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentDateDisplay => PaymentDate.ToString("dd/MM/yyyy");
    public string ClientName { get; set; } = string.Empty;
    public string ClientPhone { get; set; } = string.Empty;
    public string FactoryName { get; set; } = string.Empty;
}