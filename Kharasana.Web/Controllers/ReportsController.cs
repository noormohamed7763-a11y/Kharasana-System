using Kharasana.Application.Common;
using Kharasana.Web.Filters;
using Kharasana.Web.Localization;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.Reports;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.Web.Controllers;

[SessionAuthorize(Roles.Admin)]
public class ReportsController : BaseController
{
    private readonly IReportsApiService _reportsApiService;

    public ReportsController(IReportsApiService reportsApiService)
    {
        _reportsApiService = reportsApiService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var report = await _reportsApiService.GetReportSummaryAsync();

        if (report == null)
        {
            TempData[TempDataError] = AppMessages.Error.ReportsLoad;
            report = new ReportsViewModel();
        }

        return View(report);
    }
}