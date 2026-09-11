using System.ComponentModel.DataAnnotations;

namespace Kharasana.Domain.Entities;

public class Factory
{
    public int FactoryId { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

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

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    // Navigation Properties
    public ICollection<User> Users { get; set; } = new List<User>();

    public ICollection<ConcreteType> ConcreteTypes { get; set; } = new List<ConcreteType>();

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}