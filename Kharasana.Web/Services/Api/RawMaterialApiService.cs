using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.RawMaterials;
using Kharasana.Application.Common;
using System.Net.Http.Json;

namespace Kharasana.Web.Services.Api;

public class RawMaterialApiService : IRawMaterialService
{
    private readonly HttpClient _httpClient;

    public RawMaterialApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PagedResult<RawMaterialViewModel>> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm)
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
        var response = await _httpClient.GetFromJsonSafeAsync<PagedResult<RawMaterialViewModel>>($"api/RawMaterials?{queryString}");

        return response ?? new PagedResult<RawMaterialViewModel>
        {
            Items = [],
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = 0
        };
    }

    public async Task<RawMaterialViewModel?> GetByIdAsync(int id)
    {
        return await _httpClient.GetFromJsonSafeAsync<RawMaterialViewModel>($"api/RawMaterials/{id}");
    }

    public async Task<bool> CreateAsync(CreateRawMaterialViewModel viewModel)
    {
        var response = await _httpClient.PostAsJsonAsync("api/RawMaterials", viewModel);
        return response.IsSuccessStatusCode;
    }
}