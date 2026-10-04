using System.Text.Json.Serialization;
using Kharasana.Domain.Enums;

namespace Kharasana.Web.ViewModels.Reports;

public sealed class ReportSummaryDto
{
    [JsonPropertyName("ordersByStatus")]
    public List<OrderStatusCountDto> OrdersByStatus { get; set; } = new();

    [JsonPropertyName("ordersByConcreteType")]
    public List<ConcreteTypeCountDto> OrdersByConcreteType { get; set; } = new();

    [JsonPropertyName("totalOrders")]
    public int TotalOrders { get; set; }
}

public sealed class OrderStatusCountDto
{
    [JsonPropertyName("status")]
    public OrderStatus Status { get; set; }

    [JsonPropertyName("statusName")]
    public string StatusName { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public sealed class ConcreteTypeCountDto
{
    [JsonPropertyName("concreteTypeId")]
    public int ConcreteTypeId { get; set; }

    [JsonPropertyName("concreteTypeName")]
    public string ConcreteTypeName { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("totalQuantity")]
    public decimal TotalQuantity { get; set; }
}
