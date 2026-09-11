using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Domain.Entities;
using Kharasana.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kharasana.Infrastructure.Repositories;

public class FactoryRepository
    : GenericRepository<Factory>, IFactoryRepository
{
    public FactoryRepository(KharasanaDbContext context)
        : base(context)
    {
    }

    public async Task<IEnumerable<Factory>> GetArchivedAsync()
    {
        // IgnoreQueryFilters: تجاوز الفلتر العام (!IsDeleted) حتى لا يستبعد المؤرشفة قبل القراءة
        return await _context.Factories
            .IgnoreQueryFilters()
            .Where(f => f.IsDeleted)
            .AsNoTracking()
            .ToListAsync();
    }
}