namespace Kharasana.Application.DTOs.Report;

public sealed class ReportSummaryDto
{
    public List<OrderStatusCountDto> OrdersByStatus { get; set; } = new();
    public List<ConcreteTypeCountDto> OrdersByConcreteType { get; set; } = new();
    public int TotalOrders { get; set; }
}

public sealed class OrderStatusCountDto
{
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class ConcreteTypeCountDto
{
    public int ConcreteTypeId { get; set; }
    public string ConcreteTypeName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalQuantity { get; set; }
}
