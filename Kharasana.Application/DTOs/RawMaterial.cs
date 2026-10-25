namespace Kharasana.Application.DTOs;

public class RawMaterial
{
    public int RawMaterialId { get; set; }
    public string Name { get; set; } = null!;
    public decimal CurrentStock { get; set; }
    public string Unit { get; set; } = null!;
    public int FactoryId { get; set; }
}