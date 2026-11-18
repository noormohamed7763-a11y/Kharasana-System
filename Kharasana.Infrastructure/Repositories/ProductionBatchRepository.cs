using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Domain.Entities;
using Kharasana.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kharasana.Infrastructure.Repositories;

public class ProductionBatchRepository : GenericRepository<ProductionBatch>, IProductionBatchRepository
{
    public ProductionBatchRepository(KharasanaDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ProductionBatch>> GetAllWithDetailsAsync()
    {
        return await _context.ProductionBatches
            .Include(batch => batch.Order)
            .Include(batch => batch.ConcreteType)
            .ToListAsync();
    }

    public async Task<ProductionBatch?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.ProductionBatches
            .Include(batch => batch.Order)
            .Include(batch => batch.ConcreteType)
            .FirstOrDefaultAsync(batch => batch.ProductionBatchId == id);
    }

    public async Task<IEnumerable<ProductionBatch>> GetByFactoryIdAsync(int factoryId)
    {
        return await _context.ProductionBatches
            .Where(p => p.FactoryId == factoryId)
            .Include(p => p.Order)
            .Include(p => p.ConcreteType)
            .ToListAsync();
    }

    public async Task<IEnumerable<ProductionBatch>> GetByOrderIdAsync(int orderId)
    {
        return await _context.ProductionBatches
            .Where(p => p.OrderId == orderId)
            .Include(p => p.ConcreteType)
            .ToListAsync();
    }
}