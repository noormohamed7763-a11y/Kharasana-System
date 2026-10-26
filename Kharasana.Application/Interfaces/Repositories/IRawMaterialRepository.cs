using Kharasana.Domain.Entities;

namespace Kharasana.Application.Interfaces.Repositories;

public interface IRawMaterialRepository : IGenericRepository<RawMaterial>
{
    // أضف أي استعلامات خاصة هنا (مثل جلب المواد حسب المصنع مع المخزون)
    Task<IEnumerable<RawMaterial>> GetByFactoryIdAsync(int factoryId);

    /// <summary>
    /// جلب المواد التي وصل مخزونها للحد الأدنى أو أقل منه — للتنبيهات الاستباقية.
    /// </summary>
    Task<IEnumerable<RawMaterial>> GetLowStockAsync();
}
