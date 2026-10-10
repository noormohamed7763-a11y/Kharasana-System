using Kharasana.Domain.Entities;
using Kharasana.Application.Common;

namespace Kharasana.Application.Interfaces.Repositories;

public interface IConcreteTypeRepository : IGenericRepository<ConcreteType>
{
    Task<IEnumerable<ConcreteType>> GetAllWithFactoryAsync();

    Task<IEnumerable<ConcreteType>> GetByFactoryWithFactoryAsync(int factoryId);

    /// <summary>
    /// الأنواع <b>المحذوفة حذفًا ناعمًا</b> (المؤرشفة) مع مصانعها — لتجاوز فلتر الحذف العام.
    /// </summary>
    /// <param name="factoryId">
    /// مصنع بعينه (موظف المصنع)، أو <c>null</c> لكل المصانع (المدير).
    /// </param>
    Task<IEnumerable<ConcreteType>> GetArchivedWithFactoryAsync(int? factoryId);

    Task<ConcreteType?> GetByIdWithFactoryAsync(int id);

    Task<PagedResult<ConcreteType>> GetPagedAsync(int pageNumber, int pageSize, string? search);

    Task<PagedResult<ConcreteType>> GetPagedByFactoryAsync(int factoryId, int pageNumber, int pageSize, string? search);

    /// <summary>
    /// البحث عن نوع خرسانة <b>غير محذوف</b> بالاسم داخل المصنع — محترمًا فلتر الحذف الناعم العام.
    /// يُستخدم في فحصين: منع تكرار الاسم عند الإنشاء/التعديل، وكشف تعارض الاسم عند الاستعادة.
    /// الأسماء المحرَّرة بحذف ناعم لا تُعاد هنا لأنها متاحة لإعادة الاستخدام (خيار B)،
    /// والفهرس الفريد المُرشَّح <c>(FactoryId, Name) WHERE IsDeleted = 0</c> هو الحماية النهائية ضد السباق.
    /// المقارنة غير حساسة لحالة الأحرف (تطابق ترتيب SQL Server الافتراضي).
    /// </summary>
    /// <param name="excludeConcreteTypeId">معرّف يُستثنى من البحث (الكيان نفسه عند التعديل/الاستعادة).</param>
    Task<ConcreteType?> FindActiveByNameInFactoryAsync(
        int factoryId, string name, int? excludeConcreteTypeId = null);
}