using Kharasana.API.Common;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Factory;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.API.Controllers;

/// <summary>
/// إدارة طلبات تسجيل المصانع: قراءة، قبول، رفض.
/// - تقديم الطلب: عام (بدون مصادقة) — المصنع الجديد لا يملك حساباً بعد.
/// - القراءة والقبول والرفض: للمدير فقط.
/// </summary>
[ApiController]
[Route("api/registration-requests")]
[Authorize]
public class FactoryRegistrationRequestsController : ControllerBase
{
    private readonly IFactoryRegistrationRequestService _requestService;

    public FactoryRegistrationRequestsController(IFactoryRegistrationRequestService requestService)
    {
        _requestService = requestService;
    }

    /// <summary>تقديم طلب تسجيل مصنع جديد — عام، لا يحتاج مصادقة.</summary>
    [HttpPost]
    [AllowAnonymous]
    [ServiceFilter(typeof(ValidationFilter<RegisterFactoryDto>))]
    public async Task<IActionResult> Register([FromBody] RegisterFactoryDto dto)
    {
        var result = await _requestService.CreateRequestAsync(dto);
        if (!result.Succeeded) return BadRequest(result.Message);
        return Ok();
    }

    /// <summary>جلب جميع الطلبات (بدون ترقيم) — للمدير فقط.</summary>
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetAll()
    {
        var requests = await _requestService.GetAllRequestsAsync();
        return Ok(requests);
    }

    /// <summary>
    /// جلب الطلبات مع دعم الترقيم والبحث والفلترة بالحالة.
    /// </summary>
    [HttpGet("paged")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetPaged([FromQuery] RegistrationRequestFilterParams filter)
    {
        var result = await _requestService.GetPagedRequestsAsync(filter);
        return Ok(result);
    }

    /// <summary>تفاصيل طلب واحد بمعرّفه.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetById(int id)
    {
        var request = await _requestService.GetRequestDetailsAsync(id);
        if (request == null) return NotFound();
        return Ok(request);
    }

    /// <summary>قبول طلب تسجيل — للمدير فقط. ينشئ مصنعاً وحساباً تلقائياً.</summary>
    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Approve(int id)
    {
        var result = await _requestService.ApproveRequestAsync(id);
        if (!result.Succeeded) return BadRequest(result.Message);
        return Ok();
    }

    /// <summary>رفض طلب تسجيل مع سبب الرفض — للمدير فقط.</summary>
    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Reject(int id, [FromBody] string reason)
    {
        var result = await _requestService.RejectRequestAsync(id, reason);
        if (!result.Succeeded) return BadRequest(result.Message);
        return Ok();
    }
}
