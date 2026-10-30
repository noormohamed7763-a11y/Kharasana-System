using System.ComponentModel.DataAnnotations;
using Kharasana.Domain.Enums;

namespace Kharasana.Domain.Entities;

public class Trip
{
    public int TripId { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    public int OrderId { get; set; }
    public int FactoryId { get; set; }

    public int Sequence { get; set; }
    public decimal Quantity { get; set; }

    public int? DriverId { get; set; }
    public string? TruckPlate { get; set; }

    public bool HasPump { get; set; }
    public int? PumpDriverId { get; set; }

    public TripStatus Status { get; set; }

    public DateTime? ScheduledAt { get; set; }
    public DateTime? DepartedAt { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public DateTime? UnloadedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Navigation
    public Order Order { get; set; } = null!;
    public User? Driver { get; set; }
    public User? PumpDriver { get; set; }
    public Factory Factory { get; set; } = null!;
}
