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
        return await _apiClient.GetAsync<IEnumerable<FactoryRegistrationRequest>>("api/registration-requests")
               ?? Enumerable.Empty<FactoryRegistrationRequest>();
    }

    public async Task<PagedResult<FactoryRegistrationRequest>> GetPagedRequestsAsync(RegistrationRequestFilterParams filter)
    {
        var url = $"api/registration-requests/paged?pageNumber={filter.PageNumber}&pageSize={filter.PageSize}";

        if (!string.IsNullOrWhiteSpace(filter.Search))
            url += $"&search={Uri.EscapeDataString(filter.Search)}";

        if (filter.Status.HasValue)
            url += $"&status={(int)filter.Status.Value}";

        return await _apiClient.GetAsync<PagedResult<FactoryRegistrationRequest>>(url)
               ?? new PagedResult<FactoryRegistrationRequest>();
    }

    public async Task<FactoryRegistrationRequest?> GetByIdAsync(int id)
    {
        return await _apiClient.GetAsync<FactoryRegistrationRequest>($"api/registration-requests/{id}");
    }

    public async Task<ServiceResult> RegisterAsync(RegisterFactoryDto dto)
    {
        return await _apiClient.PostAsync<ServiceResult>("api/registration-requests", dto)
               ?? ServiceResult.Fail("فشل الاتصال بالخدمة");
    }

    public async Task<ServiceResult> ApproveAsync(int id)
    {
        return await _apiClient.PostAsync<ServiceResult>($"api/registration-requests/{id}/approve", new { })
               ?? ServiceResult.Fail("فشل الاتصال بالخدمة");
    }

    public async Task<ServiceResult> RejectAsync(int id, string reason)
    {
        return await _apiClient.PostAsync<ServiceResult>($"api/registration-requests/{id}/reject", reason)
               ?? ServiceResult.Fail("فشل الاتصال بالخدمة");
    }
}