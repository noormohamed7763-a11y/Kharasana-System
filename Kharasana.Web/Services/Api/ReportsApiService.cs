using Kharasana.Application.Common;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.Reports;
using Kharasana.Application.DTOs.Report;
using Microsoft.Extensions.Logging;

namespace Kharasana.Web.Services.Api;

public class ReportsApiService : IReportsApiService
{
    private readonly ApiClient _apiClient;
    private readonly ILogger<ReportsApiService> _logger;

    public ReportsApiService(ApiClient apiClient, ILogger<ReportsApiService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<ReportsViewModel?> GetReportSummaryAsync(int? factoryId = null)
    {
        try
        {
            var url = factoryId.HasValue
                ? $"Reports/summary?factoryId={factoryId.Value}"
                : "Reports/summary";

            var response = await _apiClient.GetAsync<ApiResponse<ReportSummaryDto>>(url);

            if (response == null || !response.Success || response.Data == null)
            {
                _logger.LogWarning("Reports API returned null or empty response.");
                return null;
            }

            return new ReportsViewModel
            {
                TotalOrders = response.Data.TotalOrders,
                OrdersByStatus = response.Data.OrdersByStatus,
                OrdersByConcreteType = response.Data.OrdersByConcreteType
            };
        }
        catch (ApiServiceException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while loading reports summary.");
            return null;
        }
    }
}
