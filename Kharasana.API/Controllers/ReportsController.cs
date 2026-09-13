using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Report;
using Kharasana.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.API.Controllers;

/// <summary>
/// تقارير الطلبات الإحصائية — مخصصة لدور Admin فقط.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// التقرير الشامل للطلبات (حسب الحالة وحسب نوع الخرسانة).
    /// </summary>
    /// <remarks>عند إرسال <paramref name="factoryId"/> يُصفّى التقرير لمصنع واحد، وحذفه يعني كل المصانع.</remarks>
    /// <param name="factoryId">معرّف المصنع لتصفية التقرير (اختياري).</param>
    /// <response code="200">تم جلب التقرير بنجاح.</response>
    /// <response code="401">التوكن غير موجود أو غير صالح.</response>
    /// <response code="403">الحساب لا يملك صلاحية المدير.</response>
    [HttpGet("summary")]
    public async Task<IActionResult> GetReportSummary([FromQuery] int? factoryId)
    {
        var result = await _reportService.GetReportSummaryAsync(factoryId);

        return Ok(new ApiResponse<ReportSummaryDto>
        {
            Success = true,
            Message = Messages.ReportsRetrievedSuccessfully,
            Data = result
        });
    }
}