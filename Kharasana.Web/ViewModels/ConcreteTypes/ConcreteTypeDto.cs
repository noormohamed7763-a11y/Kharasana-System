using System.Text.Json.Serialization;

namespace Kharasana.Web.ViewModels.ConcreteTypes;

public class ConcreteTypeDto
{
    [JsonPropertyName("concreteTypeId")]
    public int ConcreteTypeId { get; set; }

    [JsonPropertyName("factoryId")]
    public int FactoryId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("strength")]
    public int Strength { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("factoryName")]
    public string FactoryName { get; set; } = string.Empty;

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
