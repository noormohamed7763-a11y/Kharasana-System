using Asp.Versioning;
using Kharasana.API.Extensions;
using Kharasana.Application.DTOs.Trips;
using Kharasana.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/orders/{orderId}/[controller]")]
[Authorize]
public class TripsController : ControllerBase
{
    private readonly ITripService _tripService;

    public TripsController(ITripService tripService)
    {
        _tripService = tripService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTrips(int orderId)
    {
        var caller = User.GetCallerContext();
        var result = await _tripService.GetByOrderAsync(orderId, caller.UserId, caller.Role, caller.FactoryId);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(int orderId, [FromBody] CreateTripDto dto)
    {
        var caller = User.GetCallerContext();
        var result = await _tripService.CreateAsync(orderId, dto, caller.UserId, caller.Role, caller.FactoryId);
        return CreatedAtAction(nameof(GetTrips), new { orderId }, result);
    }

    [HttpPost("batch")]
    public async Task<IActionResult> CreateBatch(int orderId, [FromBody] CreateTripsBatchDto dto)
    {
        var caller = User.GetCallerContext();
        var result = await _tripService.CreateBatchAsync(orderId, dto, caller.UserId, caller.Role, caller.FactoryId);
        return Ok(result);
    }

    [HttpPatch("{tripId}/status")]
    public async Task<IActionResult> UpdateStatus(int orderId, int tripId, [FromBody] UpdateTripStatusDto dto)
    {
        var caller = User.GetCallerContext();
        // Assuming TripService logic handles the relationship to the order
        var result = await _tripService.UpdateStatusAsync(tripId, dto, caller.UserId, caller.Role, caller.FactoryId);
        return Ok(result);
    }

    [HttpDelete("{tripId}")]
    public async Task<IActionResult> Delete(int orderId, int tripId)
    {
        var caller = User.GetCallerContext();
        await _tripService.SoftDeleteAsync(tripId, caller.UserId, caller.Role, caller.FactoryId);
        return NoContent();
    }
}
