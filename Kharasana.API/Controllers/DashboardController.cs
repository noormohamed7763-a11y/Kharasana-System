using Kharasana.API.Extensions;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Dashboard;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminDashboard()
    {
        var result = await _dashboardService.GetAdminDashboardAsync();
        return Ok(new ApiResponse<AdminDashboardDto>
        {
            Success = true,
            Message = Messages.DashboardRetrievedSuccessfully,
            Data = result
        });
    }

    [HttpGet("factory")]
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> GetFactoryDashboard([FromQuery] int? factoryId)
    {
        if (!User.TryGetRole(out var currentRole))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = Messages.UserRoleNotFound,
                Data = null
            });
        }

        int targetFactoryId;

        if (currentRole == UserRole.FactoryEmployee)
        {
            var callerFactoryId = User.GetFactoryId();
            if (callerFactoryId == null)
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "لم يتم العثور على المصنع المرتبط بالمستخدم.",
                    Data = null
                });
            }
            targetFactoryId = callerFactoryId.Value;
        }
        else
        {
            if (!factoryId.HasValue)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = Messages.FactoryNotFound,
                    Data = null
                });
            }
            targetFactoryId = factoryId.Value;
        }

        var result = await _dashboardService.GetFactoryDashboardAsync(targetFactoryId);
        return Ok(new ApiResponse<FactoryDashboardDto>
        {
            Success = true,
            Message = Messages.DashboardRetrievedSuccessfully,
            Data = result
        });
    }
}