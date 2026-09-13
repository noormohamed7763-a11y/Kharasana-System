using Kharasana.API.Extensions;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Dashboard;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.API.Controllers;

/// <summary>
/// لوحة معلومات النظام: إحصائيات عامة للمدير ولوحة خاصة بكل مصنع.
/// </summary>
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

    /// <summary>
    /// جلب بيانات لوحة تحكم المدير العامة (المؤشرات الإجمالية للطلبات والمصانع والسائقين...).
    /// </summary>
    /// <remarks>مخصصة لدور Admin فقط.</remarks>
    /// <response code="200">تم جلب بيانات اللوحة بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="403">الحساب لا يملك صلاحية المدير.</response>
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

    /// <summary>
    /// جلب بيانات لوحة تحكم مصنع معيّن.
    /// </summary>
    /// <remarks>
    /// - المدير يحدّد المصنع المطلوب عبر <paramref name="factoryId"/>.
    /// - موظف المصنع لا يرسل factoryId؛ يُؤخذ مصنعه تلقائياً من التوكن ويُتجاهَل أي قيمة مرسلة.
    /// </remarks>
    /// <param name="factoryId">معرّف المصنع — إجباري للمدير، ويُتجاهَل لموظف المصنع.</param>
    /// <response code="200">تم جلب بيانات اللوحة بنجاح.</response>
    /// <response code="400">factoryId غير مُرسل من مستخدم لا يتبع مصنعاً.</response>
    /// <response code="401">التوكن غير صالح أو لا يوجد مصنع مرتبط بالحساب.</response>
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