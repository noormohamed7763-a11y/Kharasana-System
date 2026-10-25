using Kharasana.Domain.Entities;

namespace Kharasana.Application.Interfaces.Repositories;

public interface IRawMaterialRepository : IGenericRepository<RawMaterial>
{
    // أضف أي استعلامات خاصة هنا (مثل جلب المواد حسب المصنع مع المخزون)
    Task<IEnumerable<RawMaterial>> GetByFactoryIdAsync(int factoryId);
}
