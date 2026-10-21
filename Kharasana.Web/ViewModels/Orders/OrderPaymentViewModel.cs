using System.Text.Json.Serialization;
using Kharasana.Domain.Enums;

namespace Kharasana.Web.ViewModels.Orders;

public class OrderPaymentViewModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("orderId")]
    public int OrderId { get; set; }

    [JsonPropertyName("paymentDate")]
    public DateTime PaymentDate { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("paymentMethod")]
    public PaymentMethod PaymentMethod { get; set; }

    [JsonPropertyName("referenceNo")]
    public string? ReferenceNo { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}