using Kharasana.Web.ViewModels.ConcreteTypes;

namespace Kharasana.Web.Services.Interfaces;

public interface IConcreteTypeApiService
{
    Task<List<ConcreteTypeListItemViewModel>> GetAllAsync();

    /// <summary>الأنواع المؤرشفة (المحذوفة حذفًا ناعمًا) — يعزلها الـ API على مصنع موظف المصنع.</summary>
    Task<List<ConcreteTypeListItemViewModel>> GetArchivedAsync();

    Task<ConcreteTypeViewModel?> GetByIdAsync(int id);

    Task<bool> CreateAsync(CreateConcreteTypeViewModel model);

    Task<bool> UpdateAsync(int id, UpdateConcreteTypeViewModel model);

    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// استعادة نوع خرسانة مؤرشف. تفشل بـ 409 إن كان الاسم مستخدمًا بنوع غير محذوف
    /// في المصنع نفسه، فتصل الرسالة العربية من الـ API عبر <c>ApiServiceException</c>.
    /// </summary>
    Task<bool> RestoreAsync(int id);
}