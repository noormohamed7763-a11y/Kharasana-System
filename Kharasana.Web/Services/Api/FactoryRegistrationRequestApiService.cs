using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Factory;
using Kharasana.Domain.Entities;
using Kharasana.Web.Services.Interfaces;

namespace Kharasana.Web.Services.Api;

public class FactoryRegistrationRequestApiService : IFactoryRegistrationRequestApiService
{
    private readonly ApiClient _apiClient;

    public FactoryRegistrationRequestApiService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IEnumerable<FactoryRegistrationRequest>> GetAllAsync()
    {
        var response = await _apiClient.GetAsync<ApiResponse<IEnumerable<FactoryRegistrationRequest>>>("registration-requests");
        return response?.Data ?? Enumerable.Empty<FactoryRegistrationRequest>();
    }

    public async Task<PagedResult<FactoryRegistrationRequest>> GetPagedRequestsAsync(RegistrationRequestFilterParams filter)
    {
        var url = $"registration-requests/paged?pageNumber={filter.PageNumber}&pageSize={filter.PageSize}";

        if (!string.IsNullOrWhiteSpace(filter.Search))
            url += $"&search={Uri.EscapeDataString(filter.Search)}";

        if (filter.Status.HasValue)
            url += $"&status={(int)filter.Status.Value}";

        var response = await _apiClient.GetAsync<ApiResponse<PagedResult<FactoryRegistrationRequest>>>(url);
        return response?.Data ?? new PagedResult<FactoryRegistrationRequest>();
    }

    public async Task<FactoryRegistrationRequest?> GetByIdAsync(int id)
    {
        var response = await _apiClient.GetAsync<ApiResponse<FactoryRegistrationRequest>>($"registration-requests/{id}");
        return response?.Data;
    }

    public async Task<ServiceResult> RegisterAsync(RegisterFactoryDto dto)
    {
        var response = await _apiClient.PostAsync<ApiResponse<int>>("registration-requests", dto);
        return response?.Success == true
            ? ServiceResult.Ok(response.Message)
            : ServiceResult.Fail(response?.Message ?? "فشل الاتصال بالخدمة");
    }

    public async Task<ServiceResult<string>> ApproveAsync(int id)
    {
        var response = await _apiClient.PostAsync<ApiResponse<string>>($"registration-requests/{id}/approve", new { });
        if (response?.Success == true)
        {
            return ServiceResult<string>.Ok(response.Data ?? string.Empty, response.Message);
        }
        return ServiceResult<string>.Fail(response?.Message ?? "فشل الاتصال بالخدمة");
    }

    public async Task<ServiceResult> RejectAsync(int id, string reason)
    {
        var response = await _apiClient.PostAsync<ApiResponse<object>>($"registration-requests/{id}/reject", reason ?? string.Empty);
        return response?.Success == true
            ? ServiceResult.Ok(response.Message)
            : ServiceResult.Fail(response?.Message ?? "فشل الاتصال بالخدمة");
    }

    public async Task<RegistrationStatusCountsDto?> GetRegistrationStatusCountsAsync()
    {
        var response = await _apiClient.GetAsync<ApiResponse<RegistrationStatusCountsDto>>("registration-requests/status-counts");
        return response?.Data;
    }

    public async Task<int> GetPendingRequestsCountAsync()
    {
        var counts = await GetRegistrationStatusCountsAsync();
        return counts?.Pending ?? 0;
    }

    public async Task<int> GetApprovedRequestsCountAsync()
    {
        var counts = await GetRegistrationStatusCountsAsync();
        return counts?.Approved ?? 0;
    }

    public async Task<int> GetRejectedRequestsCountAsync()
    {
        var counts = await GetRegistrationStatusCountsAsync();
        return counts?.Rejected ?? 0;
    }
}