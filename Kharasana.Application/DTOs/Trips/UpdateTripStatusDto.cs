using Kharasana.Domain.Enums;

namespace Kharasana.Application.DTOs.Trips;

public class UpdateTripStatusDto
{
    public TripStatus Status { get; set; }
    public string? Notes { get; set; }
}
