using Kharasana.Application.Common;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.ConcreteTypes;
using Microsoft.Extensions.Logging;

namespace Kharasana.Web.Services.Api;

public class ConcreteTypeApiService : IConcreteTypeApiService
{
    private readonly ApiClient _apiClient;
    private readonly ILogger<ConcreteTypeApiService> _logger;

    public ConcreteTypeApiService(ApiClient apiClient, ILogger<ConcreteTypeApiService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<List<ConcreteTypeListItemViewModel>> GetAllAsync()
    {
        try
        {
            var response = await _apiClient.GetAsync<ApiResponse<List<ConcreteTypeListItemViewModel>>>("ConcreteTypes");
            return response?.Data ?? new List<ConcreteTypeListItemViewModel>();
        }
        catch (ApiServiceException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while fetching concrete types.");
            return new List<ConcreteTypeListItemViewModel>();
        }
    }

    public async Task<ConcreteTypeViewModel?> GetByIdAsync(int id)
    {
        try
        {
            var response = await _apiClient.GetAsync<ApiResponse<ConcreteTypeViewModel>>($"ConcreteTypes/{id}");
            return response?.Data;
        }
        catch (ApiServiceException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while fetching concrete type {ConcreteTypeId}.", id);
            return null;
        }
    }

    public async Task<bool> CreateAsync(CreateConcreteTypeViewModel model)
    {
        try
        {
            var response = await _apiClient.PostAsync<ApiResponse<object>>("ConcreteTypes", model);
            return response != null && response.Success;
        }
        catch (ApiServiceException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while creating concrete type.");
            return false;
        }
    }

    public async Task<bool> UpdateAsync(int id, UpdateConcreteTypeViewModel model)
    {
        try
        {
            var response = await _apiClient.PutAsync<ApiResponse<object>>($"ConcreteTypes/{id}", model);
            return response != null && response.Success;
        }
        catch (ApiServiceException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while updating concrete type {ConcreteTypeId}.", id);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var response = await _apiClient.DeleteAsync<ApiResponse<object>>($"ConcreteTypes/{id}");
            return response != null && response.Success;
        }
        catch (ApiServiceException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while deleting concrete type {ConcreteTypeId}.", id);
            return false;
        }
    }
}
