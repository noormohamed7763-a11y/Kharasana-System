using Kharasana.Web.ViewModels.Reports;

namespace Kharasana.Web.Services.Interfaces;

public interface IReportsApiService
{
    Task<ReportsViewModel?> GetReportSummaryAsync(int? factoryId = null);
}