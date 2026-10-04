using System.Text.Json.Serialization;

namespace Kharasana.Web.ViewModels.Clients;

public class CustomerSummaryDto
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("whatsApp")]
    public string? WhatsApp { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("ordersCount")]
    public int OrdersCount { get; set; }

    [JsonPropertyName("totalQuantity")]
    public decimal TotalQuantity { get; set; }

    [JsonPropertyName("lastOrderDate")]
    public DateTime? LastOrderDate { get; set; }
}
