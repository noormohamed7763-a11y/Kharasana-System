using Kharasana.Web.ViewModels.ConcreteTypes;

namespace Kharasana.Web.Services.Interfaces;

public interface IConcreteTypeApiService
{
    Task<List<ConcreteTypeListItemViewModel>> GetAllAsync();

    /// <summary>الأنواع المؤرشفة (المحذوفة حذفًا ناعمًا) — يعزلها الـ API على مصنع موظف المصنع.</summary>
    Task<List<ConcreteTypeListItemViewModel>> GetArchivedAsync();

    Task<ConcreteTypeViewModel> GetByIdAsync(int id);

    Task CreateAsync(CreateConcreteTypeViewModel model);

    Task UpdateAsync(int id, UpdateConcreteTypeViewModel model);

    Task DeleteAsync(int id);

    /// <summary>
    /// استعادة نوع خرسانة مؤرشف. تفشل بـ 409 إن كان الاسم مستخدمًا بنوع غير محذوف
    /// في المصنع نفسه، فتصل الرسالة العربية من الـ API عبر <c>ApiServiceException</c>.
    /// </summary>
    Task RestoreAsync(int id);
}