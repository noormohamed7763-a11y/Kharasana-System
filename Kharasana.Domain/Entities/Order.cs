using System.ComponentModel.DataAnnotations;
using Kharasana.Domain.Enums;

namespace Kharasana.Domain.Entities;

public class Order
{
    public int OrderId { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    public string OrderNumber { get; set; } = string.Empty;

    public int ClientId { get; set; }

    public int FactoryId { get; set; }

    public int ConcreteTypeId { get; set; }

    public string? ProjectName { get; set; }

    public string? ProjectOwnerName { get; set; }

    public string? SiteArea { get; set; }

    public string? SiteDescription { get; set; }

    public SlabType SlabType { get; set; }

    public decimal Quantity { get; set; }

    public bool NeedPump { get; set; }

    public int? FloorNumber { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public DateTime? PouringDate { get; set; }
    public TransportMethod TransportMethod { get; set; }

    public int? DriverId { get; set; }

    public string? TruckPlate { get; set; }

    public OrderStatus Status { get; set; }

    public string? Notes { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; } = false;
    // Navigation Properties

    public User Client { get; set; } = null!;

    public User? Driver { get; set; }

    public Factory Factory { get; set; } = null!;

    public ConcreteType ConcreteType { get; set; } = null!;
}