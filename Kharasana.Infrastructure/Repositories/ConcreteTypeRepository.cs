using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Domain.Entities;
using Kharasana.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kharasana.Infrastructure.Repositories;

public class ConcreteTypeRepository
    : GenericRepository<ConcreteType>, IConcreteTypeRepository
{
    // ❌ تم إزالة _context المكرر واستخدام base._context

    public ConcreteTypeRepository(KharasanaDbContext context)
        : base(context)
    {
        // ✅ لا حاجة لإعادة تعريف _context
    }

    public async Task<IEnumerable<ConcreteType>> GetAllWithFactoryAsync()
    {
        // ✅ AsNoTracking: استعلامات قرائية للعرض — تقليل الحمل على ChangeTracker
        return await _context.ConcreteTypes
            .AsNoTracking()
            .Include(x => x.Factory)
            .ToListAsync();
    }

    public async Task<IEnumerable<ConcreteType>> GetByFactoryWithFactoryAsync(int factoryId)
    {
        return await _context.ConcreteTypes
            .AsNoTracking()
            .Include(x => x.Factory)
            .Where(x => x.FactoryId == factoryId)
            .ToListAsync();
    }

    public async Task<ConcreteType?> GetByIdWithFactoryAsync(int id)
    {
        return await _context.ConcreteTypes
            .AsNoTracking()
            .Include(x => x.Factory)
            .FirstOrDefaultAsync(x => x.ConcreteTypeId == id);
    }
}