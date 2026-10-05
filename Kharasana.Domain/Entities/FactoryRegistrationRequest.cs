using Kharasana.Domain.Enums;

namespace Kharasana.Domain.Entities;

public class FactoryRegistrationRequest
{
    public int Id { get; set; }
    public string FactoryName { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string CommercialId { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;

    public RegistrationStatus Status { get; set; }
    public string? RejectionReason { get; set; }

    public int? CreatedByUserId { get; set; }
    public int? ProcessedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

    // Navigation Properties
    public int? FactoryId { get; set; }
    public Factory? Factory { get; set; }
    public User? ProcessedBy { get; set; }
}
