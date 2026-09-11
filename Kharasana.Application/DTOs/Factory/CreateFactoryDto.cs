namespace Kharasana.Application.DTOs.Factory;

public class CreateFactoryDto
{
    public string FactoryName { get; set; } = string.Empty;

    public string? OwnerName { get; set; }

    public string? Phone { get; set; }

    public string? WhatsApp { get; set; }

    public string? Email { get; set; }

    public string Area { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string? Logo { get; set; }
}