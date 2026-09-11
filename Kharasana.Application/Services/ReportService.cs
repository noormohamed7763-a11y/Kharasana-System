using Kharasana.Application.DTOs.Report;
using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Services;

namespace Kharasana.Application.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ReportSummaryDto> GetReportSummaryAsync(int? factoryId = null)
    {
        // كل الاستعلامات تُنفّذ GROUP BY في قاعدة البيانات — لا تحميل سجلات كاملة
        var byStatus = await _unitOfWork.Orders.GetCountByStatusAsync(factoryId);
        var byConcreteType = await _unitOfWork.Orders.GetCountByConcreteTypeAsync(factoryId);

        return new ReportSummaryDto
        {
            OrdersByStatus = byStatus,
            OrdersByConcreteType = byConcreteType,
            TotalOrders = byStatus.Sum(s => s.Count)
        };
    }
}