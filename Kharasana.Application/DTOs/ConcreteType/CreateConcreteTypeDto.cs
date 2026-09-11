namespace Kharasana.Application.DTOs.ConcreteType;

public class CreateConcreteTypeDto
{
    public int FactoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Strength { get; set; }

    public decimal UnitPrice { get; set; }

    public string? ImageUrl { get; set; }

    public string? Description { get; set; }
}