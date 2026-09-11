namespace Kharasana.Web.ViewModels.ConcreteTypes;

public class ConcreteTypeListItemViewModel
{
    public int ConcreteTypeId { get; set; }

    public int FactoryId { get; set; }

    public string FactoryName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Strength { get; set; }

    public decimal UnitPrice { get; set; }

    public bool IsActive { get; set; }
}