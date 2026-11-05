using Kharasana.Application.DTOs.ProductionBatch;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Interfaces.Services;

public interface IProductionBatchService
{
    Task<IEnumerable<ProductionBatchDto>> GetFactoryProductionBatchesAsync(int factoryId);
    Task<IEnumerable<ProductionBatchDto>> GetByOrderIdAsync(int orderId);
    Task<ProductionBatchDto> CreateAsync(CreateProductionBatchDto dto, int orderId);
    Task UpdateStatusAsync(int id, ProductionStatus status, int factoryId);
    Task DeleteAsync(int id, int factoryId);
}