namespace Kharasana.Web.ViewModels.RawMaterials;

public class RawMaterialViewModel
{
    public int RawMaterialId { get; set; }
    public string Name { get; set; } = null!;
    public decimal CurrentStock { get; set; }
    public string Unit { get; set; } = null!;
    public int FactoryId { get; set; }
}

public class CreateRawMaterialViewModel
{
    public string Name { get; set; } = null!;
    public decimal InitialStock { get; set; }
    public string Unit { get; set; } = null!;
}
