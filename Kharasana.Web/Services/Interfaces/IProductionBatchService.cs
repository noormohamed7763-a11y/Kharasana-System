using Kharasana.Application.Common;
using Kharasana.Web.ViewModels.ProductionBatches;

namespace Kharasana.Web.Services.Interfaces;

public interface IProductionBatchService
{
    Task<PagedResult<ProductionBatchViewModel>> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm);
    Task<ProductionBatchViewModel?> GetByIdAsync(int id);
    Task<bool> CreateAsync(CreateProductionBatchViewModel viewModel);
}
