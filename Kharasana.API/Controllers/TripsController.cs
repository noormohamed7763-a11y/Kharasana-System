using Kharasana.API.Extensions;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.Trips;
using Kharasana.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TripsController : ControllerBase
{
    private readonly ITripService _tripService;

    public TripsController(ITripService tripService)
    {
        _tripService = tripService;
    }

    // GET: api/trips/orders/{id} - جلب رحلات طلب معين
    [HttpGet("orders/{orderId}")]
    public async Task<IActionResult> GetByOrder(int orderId)
    {
        var caller = User.GetCallerContext();
        var trips = await _tripService.GetByOrderAsync(orderId, caller.UserId, caller.Role, caller.FactoryId);
        return Ok(trips);
    }

    // GET: api/trips/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        // يتطلب ITripService دالة GetByIdAsync لو أردنا جلب رحلة واحدة.
        // سنضيفها لاحقاً إذا احتجناها، حالياً نركز على سير العمل.
        return Ok();
    }

    // POST: api/trips/orders/{orderId}/trips
    [HttpPost("orders/{orderId}/trips")]
    public async Task<IActionResult> Create(int orderId, [FromBody] CreateTripDto dto)
    {
        var caller = User.GetCallerContext();
        try
        {
            var result = await _tripService.CreateAsync(orderId, dto, caller.UserId, caller.Role, caller.FactoryId);
            return CreatedAtAction(nameof(GetByOrder), new { orderId }, result);
        }
        catch (ForbiddenException ex)
        {
            return Forbid(ex.Message);
        }
        catch (BusinessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // POST: api/trips/orders/{orderId}/trips/batch
    [HttpPost("orders/{orderId}/trips/batch")]
    public async Task<IActionResult> CreateBatch(int orderId, [FromBody] CreateTripsBatchDto dto)
    {
        var caller = User.GetCallerContext();
        try
        {
            var results = await _tripService.CreateBatchAsync(orderId, dto, caller.UserId, caller.Role, caller.FactoryId);
            return Ok(results);
        }
        catch (ForbiddenException ex)
        {
            return Forbid(ex.Message);
        }
        catch (BusinessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{tripId}/status")]
    public async Task<IActionResult> UpdateStatus(int tripId, [FromBody] UpdateTripStatusDto dto)
    {
        var caller = User.GetCallerContext();
        var result = await _tripService.UpdateStatusAsync(tripId, dto, caller.UserId, caller.Role, caller.FactoryId);
        return Ok(result);
    }

    [HttpDelete("{tripId}")]
    public async Task<IActionResult> SoftDelete(int tripId)
    {
        var caller = User.GetCallerContext();
        await _tripService.SoftDeleteAsync(tripId, caller.UserId, caller.Role, caller.FactoryId);
        return NoContent();
    }
}