namespace Kharasana.Web.ViewModels.ConcreteTypes;

public class ConcreteTypeViewModel
{
    public int ConcreteTypeId { get; set; }

    public int FactoryId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string FactoryName { get; set; } = string.Empty;


    public int Strength { get; set; }

    public decimal UnitPrice { get; set; }

    public string? ImageUrl { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }
}