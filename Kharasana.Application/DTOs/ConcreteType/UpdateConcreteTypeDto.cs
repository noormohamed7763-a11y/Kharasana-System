namespace Kharasana.Application.DTOs.ConcreteType;

public class UpdateConcreteTypeDto
{
    public string Name { get; set; } = string.Empty;

    public int Strength { get; set; }

    public decimal UnitPrice { get; set; }

    public string? ImageUrl { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }
}