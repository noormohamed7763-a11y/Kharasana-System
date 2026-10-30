using System.Collections.Generic;

namespace Kharasana.Application.DTOs.Trips;

public class CreateTripsBatchDto
{
    public List<CreateTripDto> Trips { get; set; } = new List<CreateTripDto>();
}
