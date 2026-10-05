using Kharasana.Domain.Entities;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.Services.Api;
using Kharasana.Application.DTOs.Factory;
using Kharasana.Application.Common;

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
        return await _apiClient.GetAsync<IEnumerable<FactoryRegistrationRequest>>("api/registration-requests");
    }

    public async Task<IEnumerable<FactoryRegistrationRequest>> GetPendingAsync()
    {
        return await _apiClient.GetAsync<IEnumerable<FactoryRegistrationRequest>>("api/registration-requests/pending");
    }

    public async Task<FactoryRegistrationRequest?> GetByIdAsync(int id)
    {
        return await _apiClient.GetAsync<FactoryRegistrationRequest>($"api/registration-requests/{id}");
    }

    public async Task<ServiceResult> RegisterAsync(RegisterFactoryDto dto)
    {
        return await _apiClient.PostAsync<ServiceResult>("api/registration-requests", dto);
    }

    public async Task<ServiceResult> ApproveAsync(int id)
    {
        return await _apiClient.PostAsync<ServiceResult>($"api/registration-requests/{id}/approve", null);
    }

    public async Task<ServiceResult> RejectAsync(int id, string reason)
    {
        return await _apiClient.PostAsync<ServiceResult>($"api/registration-requests/{id}/reject", reason);
    }
}