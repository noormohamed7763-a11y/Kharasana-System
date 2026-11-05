using Asp.Versioning;
using Kharasana.API.Common;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Factory;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/registration-requests")]
[Route("api/registration-requests")]
[Authorize]
public class FactoryRegistrationRequestsController : ControllerBase
{
    private readonly IFactoryRegistrationRequestService _requestService;

    public FactoryRegistrationRequestsController(IFactoryRegistrationRequestService requestService)
    {
        _requestService = requestService;
    }

    [HttpPost]
    [AllowAnonymous]
    [ServiceFilter(typeof(ValidationFilter<RegisterFactoryDto>))]
    public async Task<IActionResult> Register([FromBody] RegisterFactoryDto dto)
    {
        var result = await _requestService.CreateRequestAsync(dto);
        if (!result.Succeeded) return BadRequest(ApiResponse.Fail(result.Message));
        return CreatedAtAction(nameof(GetById), new { id = result.RequestId }, ApiResponse.Ok(Messages.CreatedSuccessfully));
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetAll()
    {
        var requests = await _requestService.GetAllRequestsAsync();
        return Ok(new ApiResponse<IEnumerable<FactoryRegistrationRequestDto>>
        {
            Success = true,
            Message = Messages.FactoriesRetrievedSuccessfully,
            Data = requests
        });
    }

    [HttpGet("paged")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetPaged([FromQuery] RegistrationRequestFilterParams filter)
    {
        var result = await _requestService.GetPagedRequestsAsync(filter);
        return Ok(new ApiResponse<PagedResult<FactoryRegistrationRequestDto>>
        {
            Success = true,
            Message = Messages.FactoriesRetrievedSuccessfully,
            Data = result
        });
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetById(int id)
    {
        var request = await _requestService.GetRequestDetailsAsync(id);
        if (request == null) return NotFound(ApiResponse.Fail(Messages.FactoryNotFound));
        return Ok(new ApiResponse<FactoryRegistrationRequestDto>
        {
            Success = true,
            Message = Messages.FactoryRetrievedSuccessfully,
            Data = request
        });
    }

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Approve(int id)
    {
        var result = await _requestService.ApproveRequestAsync(id);
        if (!result.Succeeded) return BadRequest(ApiResponse.Fail(result.Message));
        return Ok(ApiResponse.Ok(Messages.CreatedSuccessfully));
    }

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Reject(int id, [FromBody] string reason)
    {
        var result = await _requestService.RejectRequestAsync(id, reason);
        if (!result.Succeeded) return BadRequest(ApiResponse.Fail(result.Message));
        return Ok(ApiResponse.Ok(Messages.OrderRejectedSuccessfully));
    }

    [HttpGet("status-counts")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetStatusCounts()
    {
        var counts = await _requestService.GetStatusCountsAsync();
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = Messages.ReportsRetrievedSuccessfully,
            Data = new { Pending = counts.Pending, Approved = counts.Approved, Rejected = counts.Rejected }
        });
    }
}
