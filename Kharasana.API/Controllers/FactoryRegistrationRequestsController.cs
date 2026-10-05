using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.API.Controllers;

[ApiController]
[Route("api/registration-requests")]
public class FactoryRegistrationRequestsController : ControllerBase
{
    private readonly IFactoryRegistrationRequestService _requestService;

    public FactoryRegistrationRequestsController(IFactoryRegistrationRequestService requestService)
    {
        _requestService = requestService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var requests = await _requestService.GetAllRequestsAsync();
        return Ok(requests);
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var requests = await _requestService.GetPendingRequestsAsync();
        return Ok(requests);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var request = await _requestService.GetRequestDetailsAsync(id);
        if (request == null) return NotFound();
        return Ok(request);
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var result = await _requestService.ApproveRequestAsync(id);
        if (!result.Succeeded) return BadRequest(result.Message);
        return Ok();
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] string reason)
    {
        var result = await _requestService.RejectRequestAsync(id, reason);
        if (!result.Succeeded) return BadRequest(result.Message);
        return Ok();
    }
}
