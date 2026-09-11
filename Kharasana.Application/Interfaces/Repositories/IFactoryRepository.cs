using Kharasana.Domain.Entities;

namespace Kharasana.Application.Interfaces.Repositories;

public interface IFactoryRepository : IGenericRepository<Factory>
{
    /// <summary>
    /// جلب المنشآت المؤرشفة فقط (IsDeleted = true) — متجاوزاً الفلتر العام للقراءة.
    /// يلزم تجاوز الفلتر لأن الاستعلام الافتراضي يستبعد المحذوف تلقائياً.
    /// </summary>
    Task<IEnumerable<Factory>> GetArchivedAsync();
}