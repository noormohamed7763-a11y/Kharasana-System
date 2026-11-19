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
    [HttpGet("orders/{orderId:int}/trips")]
    [Authorize(Roles = Roles.AdminOrFactoryEmployeeOrDriver)]
    public async Task<IActionResult> GetByOrder(int orderId)
    {
        var trips = await _tripService.GetByOrderAsync(orderId);

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
        var result = await _tripService.CreateAsync(orderId, dto);

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
        var results = await _tripService.CreateBatchAsync(orderId, dto);

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
        var result = await _tripService.UpdateStatusAsync(tripId, dto);

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
        await _tripService.SoftDeleteAsync(tripId);

        return Ok(ApiResponse.Ok(Messages.TripDeletedSuccessfully));
    }
}
