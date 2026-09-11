using Kharasana.Domain.Enums;

namespace Kharasana.Application.DTOs.Order;

public class OrderDto
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string FactoryName { get; set; } = string.Empty;
    public string ConcreteTypeName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal? TotalPrice { get; set; }
    public TransportMethod TransportMethod { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}