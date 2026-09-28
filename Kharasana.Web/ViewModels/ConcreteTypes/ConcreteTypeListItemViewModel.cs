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

    /// <summary>
    /// لحظة الأرشفة في النوع المحذوف حذفًا ناعمًا (تأتي من <c>UpdatedAt</c> في الـ API).
    /// لا تُعرض في قائمة الأنواع النشطة، بل في صفحة الأرشيف وحدها.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}