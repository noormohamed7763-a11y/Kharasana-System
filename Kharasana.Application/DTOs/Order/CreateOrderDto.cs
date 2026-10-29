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
    public string? Landmarks { get; set; }
    public string? SupervisorName { get; set; }
    public string? SupervisorPhone { get; set; }
    public CementType CementType { get; set; } = CementType.Ordinary;
    public AggregateSize AggregateSize { get; set; } = AggregateSize.Size20mm;
    public Admixture Admixture { get; set; } = Admixture.Plasticizer;
    public SlabType SlabType { get; set; }
    public decimal Quantity { get; set; }
    public bool NeedPump { get; set; }
    public int? FloorNumber { get; set; }
    public DateTime? PouringDate { get; set; }
    public TransportMethod TransportMethod { get; set; }
    public string? Notes { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
