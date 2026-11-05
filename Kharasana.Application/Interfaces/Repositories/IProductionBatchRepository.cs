using Kharasana.Domain.Entities;

namespace Kharasana.Application.Interfaces.Repositories;

public interface IProductionBatchRepository : IGenericRepository<ProductionBatch>
{
    Task<IEnumerable<ProductionBatch>> GetAllWithDetailsAsync();
    Task<ProductionBatch?> GetByIdWithDetailsAsync(int id);
    Task<IEnumerable<ProductionBatch>> GetByFactoryIdAsync(int factoryId);
    Task<IEnumerable<ProductionBatch>> GetByOrderIdAsync(int orderId);
}
