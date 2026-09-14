using System.Linq.Expressions;

namespace Kharasana.Application.Interfaces.Repositories;

public interface IGenericRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync();

    Task<T?> GetByIdAsync(int id);

    /// <summary>
    /// يقرأ الكيان حسب المعرف متجاوزاً فلاتر الاستعلام العامة (مثل IsDeleted).
    /// يُستخدم فقط عند الحاجة لاسترجاع كيان مؤرشف/محذوف — مثال: منع تسجيل دخول موظف مصنع مؤرشف، أو استعادة كيان.
    /// </summary>
    Task<T?> GetByIdIncludingDeletedAsync(int id);

    Task AddAsync(T entity);

    void Update(T entity);

    void Delete(T entity);

    Task<int> CountAsync();
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);
}