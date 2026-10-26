using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Domain.Entities;
using Kharasana.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kharasana.Infrastructure.Repositories;

public class RawMaterialRepository : GenericRepository<RawMaterial>, IRawMaterialRepository
{
    public RawMaterialRepository(KharasanaDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<RawMaterial>> GetByFactoryIdAsync(int factoryId)
    {
        return await _context.RawMaterials
            .Where(r => r.FactoryId == factoryId)
            .ToListAsync();
    }

    public async Task<IEnumerable<RawMaterial>> GetLowStockAsync()
    {
        return await _context.RawMaterials
            .Include(r => r.Factory)
            .Where(r => r.CurrentStock <= r.MinStockLevel && r.MinStockLevel > 0)
            .ToListAsync();
    }
}