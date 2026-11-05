using Kharasana.Domain.Enums;

namespace Kharasana.Application.DTOs.ProductionBatch;

public class ProductionBatchDto
{
    public int ProductionBatchId { get; set; }
    public int OrderId { get; set; }
    public int ConcreteTypeId { get; set; }
    public decimal Quantity { get; set; }
    public ProductionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateProductionBatchDto
{
    public int ConcreteTypeId { get; set; }
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
}
