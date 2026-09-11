using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Report;
using Kharasana.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.API.Controllers;

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
    /// factoryId اختياري لتصفية التقرير لمصنع معيّن، وحذفه يعني كل المصانع.
    /// </summary>
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