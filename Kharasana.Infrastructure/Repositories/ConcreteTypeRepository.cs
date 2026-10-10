using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Domain.Entities;
using Kharasana.Application.Common;
using Kharasana.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kharasana.Infrastructure.Repositories;

public class ConcreteTypeRepository
    : GenericRepository<ConcreteType>, IConcreteTypeRepository
{
    public ConcreteTypeRepository(KharasanaDbContext context)
        : base(context)
    {
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

    public async Task<IEnumerable<ConcreteType>> GetArchivedWithFactoryAsync(int? factoryId)
    {
        // IgnoreQueryFilters: تجاوز الفلتر العام (!IsDeleted) حتى لا تُستبعد المؤرشفة
        // قبل القراءة — نفس نمط FactoryRepository.GetArchivedAsync.
        var query = _context.ConcreteTypes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(x => x.Factory)
            .Where(x => x.IsDeleted);

        if (factoryId.HasValue)
        {
            query = query.Where(x => x.FactoryId == factoryId.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<ConcreteType?> FindActiveByNameInFactoryAsync(
        int factoryId, string name, int? excludeConcreteTypeId = null)
    {
        // ✅ فحص تفرّد الاسم بطريقة SARGable:
        // نعتمد على Collation قاعدة البيانات (غالباً CI - Case Insensitive) للمقارنة المباشرة،
        // أو نفرض Collation حساس للحالة باستخدام EF.Functions.Collate لضمان الأداء وعدم الحاجة لـ ToLower().
        return await _context.ConcreteTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.FactoryId == factoryId
                && x.Name == name
                && (excludeConcreteTypeId == null || x.ConcreteTypeId != excludeConcreteTypeId));
    }

    public async Task<PagedResult<ConcreteType>> GetPagedAsync(int pageNumber, int pageSize, string? search)
    {
        IQueryable<ConcreteType> query = _context.ConcreteTypes.AsNoTracking().Include(x => x.Factory);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Name.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<ConcreteType> { Items = items, TotalCount = totalCount, PageNumber = pageNumber, PageSize = pageSize };
    }

    public async Task<PagedResult<ConcreteType>> GetPagedByFactoryAsync(int factoryId, int pageNumber, int pageSize, string? search)
    {
        IQueryable<ConcreteType> query = _context.ConcreteTypes.AsNoTracking().Include(x => x.Factory).Where(x => x.FactoryId == factoryId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Name.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<ConcreteType> { Items = items, TotalCount = totalCount, PageNumber = pageNumber, PageSize = pageSize };
    }
}