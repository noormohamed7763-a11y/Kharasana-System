using System.ComponentModel.DataAnnotations;
using Kharasana.Domain.Enums;

namespace Kharasana.Web.ViewModels.ProductionBatches;

public class ProductionBatchViewModel
{
    public int ProductionBatchId { get; set; }
    public int OrderId { get; set; }
    public int ConcreteTypeId { get; set; }
    public string ConcreteTypeName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public ProductionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateProductionBatchViewModel
{
    [Range(1, int.MaxValue)]
    public int OrderId { get; set; }

    [Range(1, int.MaxValue)]
    public int ConcreteTypeId { get; set; }

    [Range(typeof(decimal), "0.01", "1000000000")]
    public decimal Quantity { get; set; }
}