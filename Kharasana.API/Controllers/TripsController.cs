using Asp.Versioning;
using Kharasana.API.Common;
using Kharasana.API.Extensions;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Trips;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
[Authorize]
public class TripsController : ControllerBase
{
    private readonly ITripService _tripService;

    public TripsController(ITripService tripService)
    {
        _tripService = tripService;
    }

    [HttpGet("orders/{orderId:int}")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployeeOrDriver)]
    public async Task<IActionResult> GetByOrder(int orderId)
    {
        var caller = User.GetCallerContext();
        var trips = await _tripService.GetByOrderAsync(orderId, caller.UserId, caller.Role, caller.FactoryId);

        return Ok(new ApiResponse<IReadOnlyList<TripDto>>
        {
            Success = true,
            Message = Messages.TripsRetrievedSuccessfully,
            Data = trips
        });
    }

    [HttpPost("orders/{orderId:int}/trips")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    [ServiceFilter(typeof(ValidationFilter<CreateTripDto>))]
    public async Task<IActionResult> Create(int orderId, [FromBody] CreateTripDto dto)
    {
        var caller = User.GetCallerContext();
        var result = await _tripService.CreateAsync(orderId, dto, caller.UserId, caller.Role, caller.FactoryId);

        return CreatedAtAction(
            nameof(GetByOrder),
            new { orderId },
            new ApiResponse<TripDto>
            {
                Success = true,
                Message = Messages.TripCreatedSuccessfully,
                Data = result
            });
    }

    [HttpPost("orders/{orderId:int}/trips/batch")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    [ServiceFilter(typeof(ValidationFilter<CreateTripsBatchDto>))]
    public async Task<IActionResult> CreateBatch(int orderId, [FromBody] CreateTripsBatchDto dto)
    {
        var caller = User.GetCallerContext();
        var results = await _tripService.CreateBatchAsync(orderId, dto, caller.UserId, caller.Role, caller.FactoryId);

        return Ok(new ApiResponse<IReadOnlyList<TripDto>>
        {
            Success = true,
            Message = Messages.TripsCreatedSuccessfully,
            Data = results
        });
    }

    [HttpPatch("{tripId:int}/status")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployeeOrDriver)]
    [ServiceFilter(typeof(ValidationFilter<UpdateTripStatusDto>))]
    public async Task<IActionResult> UpdateStatus(int tripId, [FromBody] UpdateTripStatusDto dto)
    {
        var caller = User.GetCallerContext();
        var result = await _tripService.UpdateStatusAsync(tripId, dto, caller.UserId, caller.Role, caller.FactoryId);

        return Ok(new ApiResponse<TripDto>
        {
            Success = true,
            Message = Messages.TripStatusUpdatedSuccessfully,
            Data = result
        });
    }

    [HttpDelete("{tripId:int}")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> SoftDelete(int tripId)
    {
        var caller = User.GetCallerContext();
        await _tripService.SoftDeleteAsync(tripId, caller.UserId, caller.Role, caller.FactoryId);

        return Ok(ApiResponse.Ok(Messages.TripDeletedSuccessfully));
    }
}
