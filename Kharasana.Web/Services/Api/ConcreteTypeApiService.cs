using Kharasana.Application.Common;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.ConcreteTypes;
using Microsoft.Extensions.Logging;
using System.Net;

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

    public async Task<PagedResult<ConcreteTypeListItemViewModel>> GetAllAsync(int pageNumber = 1, int pageSize = 20, string? search = null)
    {
        var query = $"ConcreteTypes?pageNumber={pageNumber}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            query += $"&search={Uri.EscapeDataString(search)}";

        var response = await _apiClient.GetAsync<ApiResponse<PagedResult<ConcreteTypeListItemViewModel>>>(query);
        if (response == null || !response.Success)
        {
            throw new ApiServiceException(HttpStatusCode.InternalServerError, ApiErrorCatalog.ServerError);
        }
        return response.Data ?? new PagedResult<ConcreteTypeListItemViewModel>();
    }

    public async Task<List<ConcreteTypeListItemViewModel>> GetArchivedAsync()
    {
        var response = await _apiClient.GetAsync<ApiResponse<List<ConcreteTypeListItemViewModel>>>("ConcreteTypes/archived");
        if (response == null || !response.Success)
        {
            throw new ApiServiceException(HttpStatusCode.InternalServerError, ApiErrorCatalog.ServerError);
        }
        return response.Data ?? new List<ConcreteTypeListItemViewModel>();
    }

    public async Task<ConcreteTypeViewModel> GetByIdAsync(int id)
    {
        var response = await _apiClient.GetAsync<ApiResponse<ConcreteTypeViewModel>>($"ConcreteTypes/{id}");
        if (response == null || !response.Success || response.Data == null)
        {
            throw new ApiServiceException(HttpStatusCode.NotFound, ApiErrorCatalog.ConcreteTypeNotFound, new object[] { id });
        }
        return response.Data;
    }

    public async Task CreateAsync(CreateConcreteTypeViewModel model)
    {
        var response = await _apiClient.PostAsync<ApiResponse<object>>("ConcreteTypes", model);
        if (response == null || !response.Success)
        {
            throw new ApiServiceException(HttpStatusCode.BadRequest, ApiErrorCatalog.ConcreteTypeCreateFailed, new object[] { response?.Message ?? "سبب غير معروف" });
        }
    }

    public async Task UpdateAsync(int id, UpdateConcreteTypeViewModel model)
    {
        var response = await _apiClient.PutAsync<ApiResponse<object>>($"ConcreteTypes/{id}", model);
        if (response == null || !response.Success)
        {
            throw new ApiServiceException(HttpStatusCode.BadRequest, ApiErrorCatalog.ConcreteTypeUpdateFailed, new object[] { id, response?.Message ?? "سبب غير معروف" });
        }
    }

    public async Task DeleteAsync(int id)
    {
        var response = await _apiClient.DeleteAsync<ApiResponse<object>>($"ConcreteTypes/{id}");
        if (response == null || !response.Success)
        {
            throw new ApiServiceException(HttpStatusCode.BadRequest, ApiErrorCatalog.ConcreteTypeDeleteFailed, new object[] { id, response?.Message ?? "سبب غير معروف" });
        }
    }

    public async Task RestoreAsync(int id)
    {
        var response = await _apiClient.PostAsync<ApiResponse<object>>($"ConcreteTypes/restore/{id}", new { });
        if (response == null || !response.Success)
        {
            throw new ApiServiceException(HttpStatusCode.BadRequest, ApiErrorCatalog.ServerError);
        }
    }
}