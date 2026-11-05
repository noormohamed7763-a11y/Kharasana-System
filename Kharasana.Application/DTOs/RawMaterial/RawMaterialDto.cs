namespace Kharasana.Application.DTOs.RawMaterial;

public class RawMaterialDto
{
    public int RawMaterialId { get; set; }
    public string Name { get; set; } = null!;
    public decimal CurrentStock { get; set; }
    public string Unit { get; set; } = null!;
    public int FactoryId { get; set; }
}

public class CreateRawMaterialDto
{
    public string Name { get; set; } = null!;
    public decimal CurrentStock { get; set; }
    public string Unit { get; set; } = null!;
    public int? FactoryId { get; set; }
}

public class UpdateRawMaterialStockDto
{
    public decimal QuantityChange { get; set; }
}
