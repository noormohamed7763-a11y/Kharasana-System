namespace Kharasana.Domain.Entities;

public class ConcreteType
{
    public int ConcreteTypeId { get; set; }

    public int FactoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Strength { get; set; }

    public decimal UnitPrice { get; set; }

    public string? ImageUrl { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; } = false;
    // Navigation Properties

    public Factory Factory { get; set; } = null!;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}