using System.Collections.Generic;
using Kharasana.Application.DTOs.Trips;

namespace Kharasana.Application.DTOs.Trips;

public class OrderTripsSummaryDto
{
    public int OrderId { get; set; }
    public decimal OrderQuantity { get; set; }
    public decimal TripsTotalQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public int TripsCount { get; set; }
    public int CompletedTripsCount { get; set; }
    public List<TripDto> Trips { get; set; } = new List<TripDto>();
}
