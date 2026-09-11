using Kharasana.Domain.Enums;

namespace Kharasana.Application.DTOs.Order;

public class OrderDetailsDto
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;

    // ✅ معلومات العميل
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string? ClientPhone { get; set; }
    public int ClientOrdersCount { get; set; }

    // ✅ معلومات المصنع
    public int FactoryId { get; set; }
    public string FactoryName { get; set; } = string.Empty;

    // ✅ معلومات نوع الخرسانة
    public int ConcreteTypeId { get; set; }
    public string ConcreteTypeName { get; set; } = string.Empty;

    // ✅ معلومات المشروع
    public string? ProjectName { get; set; }
    public string? ProjectOwnerName { get; set; }
    public string? SiteArea { get; set; }
    public string? SiteDescription { get; set; }
    public SlabType SlabType { get; set; }

    // ✅ معلومات الكمية والسعر
    public decimal Quantity { get; set; }
    public bool NeedPump { get; set; }
    public int? FloorNumber { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? TotalPrice { get; set; }
    public DateTime? PouringDate { get; set; }

    // ✅ معلومات النقل
    public TransportMethod TransportMethod { get; set; }

    // ✅ معلومات السائق
    public int? DriverId { get; set; }
    public string? DriverName { get; set; }
    public string? TruckPlate { get; set; }

    // ✅ معلومات الحالة
    public OrderStatus Status { get; set; }
    public string? Notes { get; set; }
}