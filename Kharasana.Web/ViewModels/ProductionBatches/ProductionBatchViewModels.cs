namespace Kharasana.Web.ViewModels.ProductionBatches;

public class ProductionBatchViewModel
{
    public int ProductionBatchId { get; set; }
    public string BatchNumber { get; set; } = null!;
    public DateTime ProductionDate { get; set; }
    public int ConcreteTypeId { get; set; }
    public string ConcreteTypeName { get; set; } = null!;
    public decimal PlannedQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public int FactoryId { get; set; }
}

public class CreateProductionBatchViewModel
{
    public string BatchNumber { get; set; } = null!;
    public DateTime ProductionDate { get; set; }
    public int ConcreteTypeId { get; set; }
    public decimal PlannedQuantity { get; set; }
}