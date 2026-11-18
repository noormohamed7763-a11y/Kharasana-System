using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.RawMaterials;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.RawMaterial;

namespace Kharasana.Web.Services.Api;

public class RawMaterialApiService : IRawMaterialService
{
    private readonly ApiClient _apiClient;

    public RawMaterialApiService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<PagedResult<RawMaterialViewModel>> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm)
    {
        var response = await _apiClient.GetAsync<ApiResponse<IEnumerable<RawMaterialDto>>>("RawMaterials");
        var materials = response?.Success == true && response.Data != null
            ? response.Data.Select(Map).ToList()
            : [];

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            materials = materials
                .Where(material => material.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var totalCount = materials.Count;

        return new PagedResult<RawMaterialViewModel>
        {
            Items = materials.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<RawMaterialViewModel?> GetByIdAsync(int id)
    {
        var response = await _apiClient.GetAsync<ApiResponse<RawMaterialDto>>($"RawMaterials/{id}");
        return response?.Success == true && response.Data != null ? Map(response.Data) : null;
    }

    public async Task<bool> CreateAsync(CreateRawMaterialViewModel viewModel)
    {
        var response = await _apiClient.PostAsync<ApiResponse<RawMaterialDto>>(
            "RawMaterials",
            new CreateRawMaterialDto
            {
                Name = viewModel.Name,
                CurrentStock = viewModel.InitialStock,
                Unit = viewModel.Unit,
                FactoryId = viewModel.FactoryId
            });

        return response?.Success == true;
    }

    private static RawMaterialViewModel Map(RawMaterialDto material) => new()
    {
        RawMaterialId = material.RawMaterialId,
        Name = material.Name,
        CurrentStock = material.CurrentStock,
        Unit = material.Unit,
        FactoryId = material.FactoryId
    };
}