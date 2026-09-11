using Kharasana.Application.DTOs.Report;

namespace Kharasana.Application.Interfaces.Services;

public interface IReportService
{
    /// <summary>
    /// Returns the report summary (orders by status and by concrete type).
    /// When <paramref name="factoryId"/> is null, the report covers all factories.
    /// </summary>
    Task<ReportSummaryDto> GetReportSummaryAsync(int? factoryId = null);
}