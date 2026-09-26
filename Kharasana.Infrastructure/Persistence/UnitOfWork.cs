using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kharasana.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly KharasanaDbContext _context;

    public IFactoryRepository Factories { get; }

    public IUserRepository Users { get; }

    public IConcreteTypeRepository ConcreteTypes { get; }

    public IOrderRepository Orders { get; }

    public UnitOfWork(KharasanaDbContext context)
    {
        _context = context;

        Factories = new FactoryRepository(context);
        Users = new UserRepository(context);
        ConcreteTypes = new ConcreteTypeRepository(context);
        Orders = new OrderRepository(context);
    }

    public virtual async Task<int> SaveChangesAsync()
    {
        try
        {
            return await SaveChangesInternalAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(Messages.ConcurrencyConflict);
        }
        catch (DbUpdateException ex) when (UniqueConstraintDetector.IsUniqueViolation(ex))
        {
            // ✅ الحماية النهائية ضد السباق: فحص الخدمة المسبق قد يمرّ قبل أن يسجّل طلب آخر
            //    نفس القيمة، فيبقى فهرس التفرّد في قاعدة البيانات هو الحكم — ويُترجم إلى 409.
            //    ملاحظة: هذا الالتقاط مقصور على انتهاك التفرّد (2601/2627) فقط؛
            //    أخطاء المفاتيح الأجنبية وغيرها تبقى 500 كما كانت.
            throw new ConflictException(Messages.DuplicateValueConflict);
        }
    }

    protected virtual async Task<int> SaveChangesInternalAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        // تُدار دورة حياة KharasanaDbContext عبر حاوية الـ DI لتجنب ObjectDisposedException
    }
}