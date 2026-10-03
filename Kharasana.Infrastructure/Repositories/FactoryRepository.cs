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

    public async Task<bool> FactoryNameExistsAsync(string factoryName, int? excludeFactoryId = null)
    {
        // IgnoreQueryFilters: فهرس التفرّد على FactoryName يشمل المؤرشفة، فالفحص المسبق يطابقه
        //
        // ToLower() ضرورة لا سهو: التطبيق يعمل بمزوّدَيْن — SQL Server
        // (ترتيب افتراضي غير حسّاس لحالة الأحرف) وInMemory (حسّاس). بدون
        // التطبيع يختلف سلوك المزوّدين وتنهار اختبارات التفرّد
        // (مثل Create_DuplicateNameWithDifferentCase). التطبيع يوحّد السلوك
        // عبر المزوّدين، وفهرس التفرّد يبقى الحماية النهائية ضد السباق.
        // LOWER(col) غير قابل لاستخدام الفهرس (non-sargable) في SQL Server،
        // لكن FactoryName قيد صغير (عشرات المصانع) فلا كلفة عملية.
        var normalized = factoryName.ToLower();

        return await _context.Factories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(f =>
                f.FactoryName.ToLower() == normalized
                && (excludeFactoryId == null || f.FactoryId != excludeFactoryId));
    }
}