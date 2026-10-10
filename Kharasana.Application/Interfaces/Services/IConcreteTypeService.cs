using Kharasana.Application.DTOs.ConcreteType;
using Kharasana.Application.Common;

namespace Kharasana.Application.Interfaces.Services;

public interface IConcreteTypeService
{
    Task<PagedResult<ConcreteTypeDto>> GetAllAsync(int pageNumber = 1, int pageSize = 20, string? search = null, int? factoryId = null);

    /// <summary>الأنواع المحذوفة حذفًا ناعمًا (المؤرشفة) — عكس <see cref="GetAllAsync"/> الذي يستبعدها.</summary>
    /// <param name="factoryId">مصنع بعينه (موظف المصنع)، أو <c>null</c> لكل المصانع (المدير).</param>
    Task<IEnumerable<ConcreteTypeDto>> GetArchivedAsync(int? factoryId = null);

    Task<ConcreteTypeDto> GetByIdAsync(int id, int? currentFactoryId = null);
    Task<ConcreteTypeDto> CreateAsync(CreateConcreteTypeDto dto, int? currentFactoryId = null);
    Task<bool> UpdateAsync(int id, UpdateConcreteTypeDto dto, int? currentFactoryId = null);

    /// <summary>حذف ناعم لنوع الخرسانة — يبقى الصف لتظل الطلبات التاريخية سليمة.</summary>
    /// <param name="currentFactoryId">مصنع المتصل (يُمرَّر لموظف المصنع فقط) لفرض العزل.</param>
    Task<bool> DeleteAsync(int id, int? currentFactoryId = null);

    /// <summary>استعادة نوع خرسانة محذوفًا ناعمًا — يفشل بـ 409 إن كان الاسم مستخدمًا بنوع غير محذوف.</summary>
    /// <param name="currentFactoryId">مصنع المتصل (يُمرَّر لموظف المصنع فقط) لفرض العزل.</param>
    Task<bool> RestoreAsync(int id, int? currentFactoryId = null);
}