using Kharasana.Domain.Enums;

namespace Kharasana.Application.DTOs.Trips;

public class TripDto
{
    public int TripId { get; set; }
    public int OrderId { get; set; }
    public int Sequence { get; set; }
    public decimal Quantity { get; set; }
    public int? DriverId { get; set; }
    public string? DriverName { get; set; }
    public string? TruckPlate { get; set; }
    public bool HasPump { get; set; }
    public TripStatus Status { get; set; }
    public string StatusNameAr { get; set; } = "";
    public DateTime? ScheduledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
