using Kharasana.Domain.Entities;

namespace Kharasana.Application.Interfaces.Repositories;

public interface IFactoryRepository : IGenericRepository<Factory>
{
    /// <summary>
    /// جلب المنشآت المؤرشفة فقط (IsDeleted = true) — متجاوزاً الفلتر العام للقراءة.
    /// يلزم تجاوز الفلتر لأن الاستعلام الافتراضي يستبعد المحذوف تلقائياً.
    /// </summary>
    Task<IEnumerable<Factory>> GetArchivedAsync();

    /// <summary>
    /// هل اسم المصنع مستخدم؟ — متجاوزًا فلتر الحذف الناعم عمدًا، لأن فهرس التفرّد على
    /// FactoryName يشمل المصانع المؤرشفة أيضًا. المقارنة غير حساسة لحالة الأحرف.
    /// </summary>
    /// <param name="excludeFactoryId">معرّف يُستثنى من البحث (المصنع نفسه عند التعديل).</param>
    Task<bool> FactoryNameExistsAsync(string factoryName, int? excludeFactoryId = null);
}