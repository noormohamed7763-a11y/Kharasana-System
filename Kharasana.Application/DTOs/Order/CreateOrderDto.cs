using Kharasana.Domain.Enums;

namespace Kharasana.Application.DTOs.Order;

public class CreateOrderDto
{
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
    public DateTime? PouringDate { get; set; }
    public TransportMethod TransportMethod { get; set; }
    public string? Notes { get; set; }
}