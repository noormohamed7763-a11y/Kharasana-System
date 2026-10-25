using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.ProductionBatches;
using Kharasana.Application.Common;
using System.Net.Http.Json;

namespace Kharasana.Web.Services.Api;

public class ProductionBatchApiService : IProductionBatchService
{
    private readonly HttpClient _httpClient;

    public ProductionBatchApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PagedResult<ProductionBatchViewModel>> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm)
    {
        var queryParams = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            queryParams.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");
        }

        var queryString = string.Join("&", queryParams);
        var response = await _httpClient.GetFromJsonSafeAsync<PagedResult<ProductionBatchViewModel>>($"api/ProductionBatches?{queryString}");

        return response ?? new PagedResult<ProductionBatchViewModel>
        {
            Items = [],
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = 0
        };
    }

    public async Task<ProductionBatchViewModel?> GetByIdAsync(int id)
    {
        return await _httpClient.GetFromJsonSafeAsync<ProductionBatchViewModel>($"api/ProductionBatches/{id}");
    }

    public async Task<bool> CreateAsync(CreateProductionBatchViewModel viewModel)
    {
        var response = await _httpClient.PostAsJsonAsync("api/ProductionBatches", viewModel);
        return response.IsSuccessStatusCode;
    }
}
