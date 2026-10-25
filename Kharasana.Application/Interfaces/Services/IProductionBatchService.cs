using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Interfaces.Services;

public interface IProductionBatchService
{
    Task<IEnumerable<ProductionBatch>> GetFactoryProductionBatchesAsync(int factoryId);
    Task<IEnumerable<ProductionBatch>> GetByOrderIdAsync(int orderId);
    Task<ProductionBatch> CreateAsync(ProductionBatch batch, int orderId);
    Task UpdateStatusAsync(int id, ProductionStatus status, int factoryId);
    Task DeleteAsync(int id, int factoryId);
}