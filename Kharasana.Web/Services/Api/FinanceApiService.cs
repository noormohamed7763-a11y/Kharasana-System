using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Order;
using Kharasana.Web.Services.Interfaces;

namespace Kharasana.Web.Services.Api;

public class FinanceApiService : IFinanceApiService
{
    private readonly ApiClient _apiClient;

    public FinanceApiService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<PagedResult<OrderPaymentDto>> GetPendingPaymentsAsync(int factoryId, int pageNumber = 1, int pageSize = 10)
    {
        var response = await _apiClient.GetAsync<ApiResponse<IEnumerable<OrderPaymentDto>>>($"Finance/pending?factoryId={factoryId}");
        var payments = response?.Success == true && response.Data != null
            ? response.Data.ToList()
            : [];

        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var totalCount = payments.Count;

        return new PagedResult<OrderPaymentDto>
        {
            Items = payments.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<bool> ApprovePaymentAsync(int paymentId, string verificationNotes)
    {
        var response = await _apiClient.PostAsync<ApiResponse<bool>>($"Finance/{paymentId}/approve", verificationNotes);
        return response?.Success == true;
    }

    public async Task<bool> RejectPaymentAsync(int paymentId, string rejectionReason)
    {
        var response = await _apiClient.PostAsync<ApiResponse<bool>>($"Finance/{paymentId}/reject", rejectionReason);
        return response?.Success == true;
    }
}
