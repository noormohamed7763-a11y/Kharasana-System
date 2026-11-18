using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.ProductionBatches;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.ProductionBatch;

namespace Kharasana.Web.Services.Api;

public class ProductionBatchApiService : IProductionBatchService
{
    private readonly ApiClient _apiClient;

    public ProductionBatchApiService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<PagedResult<ProductionBatchViewModel>> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm)
    {
        var response = await _apiClient.GetAsync<ApiResponse<IEnumerable<ProductionBatchDto>>>("ProductionBatches");
        var batches = response?.Success == true && response.Data != null
            ? response.Data.Select(Map).ToList()
            : [];

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            batches = batches
                .Where(batch => batch.OrderId.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                    || batch.ConcreteTypeName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var totalCount = batches.Count;

        return new PagedResult<ProductionBatchViewModel>
        {
            Items = batches.OrderByDescending(batch => batch.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ProductionBatchViewModel?> GetByIdAsync(int id)
    {
        var response = await _apiClient.GetAsync<ApiResponse<ProductionBatchDto>>($"ProductionBatches/{id}");
        return response?.Success == true && response.Data != null ? Map(response.Data) : null;
    }

    public async Task<bool> CreateAsync(CreateProductionBatchViewModel viewModel)
    {
        var response = await _apiClient.PostAsync<ApiResponse<ProductionBatchDto>>(
            $"ProductionBatches/order/{viewModel.OrderId}",
            new CreateProductionBatchDto
            {
                ConcreteTypeId = viewModel.ConcreteTypeId,
                Quantity = viewModel.Quantity
            });

        return response?.Success == true;
    }

    private static ProductionBatchViewModel Map(ProductionBatchDto batch) => new()
    {
        ProductionBatchId = batch.ProductionBatchId,
        OrderId = batch.OrderId,
        ConcreteTypeId = batch.ConcreteTypeId,
        ConcreteTypeName = batch.ConcreteTypeName,
        Quantity = batch.Quantity,
        Status = batch.Status,
        CreatedAt = batch.CreatedAt
    };
}
