using Kharasana.Domain.Enums;

namespace Kharasana.Application.DTOs.Order;

public class OrderDto
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    /// <summary>هاتف العميل — تعرضه قائمة الطلبات في طبقة الويب تحت اسم العميل.</summary>
    public string? ClientPhone { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public string FactoryName { get; set; } = string.Empty;
    public string ConcreteTypeName { get; set; } = string.Empty;
    public int? DriverId { get; set; }
    public decimal Quantity { get; set; }
    public decimal? TotalPrice { get; set; }
    public TransportMethod TransportMethod { get; set; }
    public OrderStatus Status { get; set; }
    /// <summary>تاريخ الصب — تعرضه قائمة الطلبات في طبقة الويب (مع شارة «اليوم»).</summary>
    public DateTime? PouringDate { get; set; }
    public DateTime CreatedAt { get; set; }
}