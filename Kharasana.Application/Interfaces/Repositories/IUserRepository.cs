using Kharasana.Application.Common;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Interfaces.Repositories;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByPhoneAsync(string phone);
    /// <summary>
    /// هل البريد الإلكتروني مستخدم؟ <paramref name="excludeUserId"/> يستثني المستخدم نفسه
    /// عند التعديل — بنفس نمط <see cref="FactoryHasAccountAsync"/>، وبدونه يفشل حفظ أي
    /// تعديل لمستخدم يملك بريداً لأنه يصطدم ببريده الحالي.
    /// </summary>
    Task<bool> EmailExistsAsync(string email, int? excludeUserId = null);
    Task<bool> PhoneExistsAsync(string phone);

    /// <summary>
    /// هل للمصنع حساب موظف؟ <paramref name="excludeUserId"/> يستثني المستخدم نفسه عند
    /// التعديل — نفس دور المعامل في <see cref="EmailExistsAsync"/>.
    /// </summary>
    Task<bool> FactoryHasAccountAsync(int factoryId, int? excludeUserId = null);

    /// <summary>
    /// إرجاع مصانع المعرّفات التي تملك حساب موظف مصنع — استعلام واحد دفعةً بدلاً من N استعلامات.
    /// </summary>
    Task<HashSet<int>> GetFactoryIdsWithEmployeeAsync();

    /// <summary>
    /// قائمة مستخدمين مرقّمة مع فلاتر. <paramref name="isActive"/> يُصفّي الحسابات
    /// الموقوفة على الخادم — بدونه كانت قوائم الاختيار تجلبه ثم تُصفّيه في الويب،
    /// فيتضخّم عدد الصفحات وقد لا يصل العنصر المطلوب ضمن الصفحة المجلوبة.
    /// </summary>
    Task<PagedResult<User>> GetPagedAsync(
        UserRole? role, int? factoryId, DriverStatus? driverStatus, string? search,
        int pageNumber, int pageSize, bool? isActive = null);
}