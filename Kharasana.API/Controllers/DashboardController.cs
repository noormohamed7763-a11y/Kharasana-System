using Asp.Versioning;
using Kharasana.API.Common;
using Kharasana.API.Extensions;
using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
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
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
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
    [Authorize(Roles = Roles.Admin)]
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
    [Authorize(Roles = Roles.AdminOrFactoryEmployee)]
    public async Task<IActionResult> GetFactoryDashboard([FromQuery] int? factoryId)
    {
        var caller = User.GetCallerContext();

        int targetFactoryId;

        if (caller.Role == UserRole.FactoryEmployee)
        {
            if (caller.FactoryId is null)
                throw new UnauthorizedException(Messages.FactoryNotFoundForUser);
            targetFactoryId = caller.FactoryId.Value;
        }
        else
        {
            if (!factoryId.HasValue)
            {
                return BadRequest(ApiResponse.Fail(Messages.FactoryNotFound));
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