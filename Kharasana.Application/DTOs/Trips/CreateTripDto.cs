using Kharasana.Domain.Enums;

namespace Kharasana.Application.DTOs.Trips;

public class CreateTripDto
{
    public decimal Quantity { get; set; }
    public int? DriverId { get; set; }
    public string? TruckPlate { get; set; }
    public bool HasPump { get; set; }
    public int? PumpDriverId { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public string? Notes { get; set; }
}
