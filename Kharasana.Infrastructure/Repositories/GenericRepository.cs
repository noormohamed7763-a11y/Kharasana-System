using System.Linq.Expressions;
using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Kharasana.Infrastructure.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly KharasanaDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(KharasanaDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        // ✅ AsNoTracking: استعلام قرائي للعرض — لا حاجة لتتبع الكيانات (يقلّل الحمل على ChangeTracker)
        return await _dbSet.AsNoTracking().ToListAsync();
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        // ✅ Use LINQ instead of FindAsync to respect global query filters (e.g., IsDeleted)
        var pk = _context.Model.FindEntityType(typeof(T))!.FindPrimaryKey()!;
        var param = Expression.Parameter(typeof(T), "e");
        var property = Expression.Property(param, pk.Properties[0].Name);
        var constant = Expression.Constant(id);
        var equal = Expression.Equal(property, constant);
        var lambda = Expression.Lambda<Func<T, bool>>(equal, param);
        return await _dbSet.FirstOrDefaultAsync(lambda);
    }

    public async Task<T?> GetByIdIncludingDeletedAsync(int id)
    {
        // ✅ يتجاوز فلاتر الاستعلام العامة عبر IgnoreQueryFilters — للقراءة المتعمّدة للكيانات المؤرشفة
        var pk = _context.Model.FindEntityType(typeof(T))!.FindPrimaryKey()!;
        var param = Expression.Parameter(typeof(T), "e");
        var property = Expression.Property(param, pk.Properties[0].Name);
        var constant = Expression.Constant(id);
        var equal = Expression.Equal(property, constant);
        var lambda = Expression.Lambda<Func<T, bool>>(equal, param);
        return await _dbSet.IgnoreQueryFilters().FirstOrDefaultAsync(lambda);
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public void Update(T entity)
    {
        var entry = _context.Entry(entity);

        if (entry.State != EntityState.Detached)
        {
            // كيان مُتتبع بالفعل — EF يكشف التغييرات تلقائياً عبر SaveChanges دون إعادة إرفاق
            return;
        }

        // كيان غير مُتتبع لكن يوجد مثيل آخر مُتتبع بنفس المفتاح
        // (حدث شائع مع InMemory: إضافة الكيان ثم تحميله بنسخة AsNoTracking في نفس السياق)
        // ننسخ القيم إلى المثيل المُتتبع بدلاً من محاولة إرفاق نسخة ثانية — يتعذّر EF إرفاق نسختين بمفتاح واحد
        var tracked = FindTrackedByKey(entry);
        if (tracked != null)
        {
            _context.Entry(tracked).CurrentValues.SetValues(entity);
            return;
        }

        _dbSet.Update(entity);
    }

    private T? FindTrackedByKey(EntityEntry<T> entry)
    {
        if (entry.Metadata.FindPrimaryKey() is not { } pk)
            return default;

        var keyProperties = pk.Properties.ToArray();

        foreach (var other in _context.ChangeTracker.Entries<T>())
        {
            if (other.State == EntityState.Detached)
                continue;

            var sameKey = keyProperties.All(p =>
                object.Equals(
                    entry.Property(p.Name).CurrentValue,
                    other.Property(p.Name).CurrentValue));

            if (sameKey)
                return other.Entity;
        }

        return default;
    }

    public void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }

    public async Task<int> CountAsync()
    {
        return await _dbSet.CountAsync();
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.CountAsync(predicate);
    }
}