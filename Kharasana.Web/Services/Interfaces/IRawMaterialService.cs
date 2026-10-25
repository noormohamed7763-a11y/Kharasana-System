using Kharasana.Application.Common;
using Kharasana.Web.ViewModels.RawMaterials;

namespace Kharasana.Web.Services.Interfaces;

public interface IRawMaterialService
{
    Task<PagedResult<RawMaterialViewModel>> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm);
    Task<RawMaterialViewModel?> GetByIdAsync(int id);
    Task<bool> CreateAsync(CreateRawMaterialViewModel viewModel);
}
