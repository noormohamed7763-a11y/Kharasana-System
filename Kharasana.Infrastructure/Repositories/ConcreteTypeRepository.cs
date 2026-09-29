using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Domain.Entities;
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
        // فلتر الحذف الناعم العام (!IsDeleted) مطبَّق هنا عمدًا:
        // الأسماء المحرَّرة بحذف ناعم متاحة لإعادة الاستخدام (خيار B)،
        // فلا يعارض الإنشاء/التعديل إلا نوع غير محذوف بالاسم نفسه.
        // المقارنة غير حساسة لحالة الأحرف لتطابق ترتيب SQL Server الافتراضي
        // وتطابق إنفاذ الفهرس المُرشَّح (WHERE IsDeleted = 0) وهو الحماية النهائية ضد السباق.
        var normalized = name.ToLower();

        return await _context.ConcreteTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.FactoryId == factoryId
                && x.Name.ToLower() == normalized
                && (excludeConcreteTypeId == null || x.ConcreteTypeId != excludeConcreteTypeId));
    }
}