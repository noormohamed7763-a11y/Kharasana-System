using Kharasana.Application.Common;
using Kharasana.Web.Models.Users;
using Kharasana.Web.ViewModels.Users;

namespace Kharasana.Web.Services.Interfaces;

public interface IUserApiService
{
    Task<ApiResponse<object>> CreateAsync(CreateUserViewModel model);

    Task<PagedResult<UserListItemViewModel>?> GetUsersAsync(
        int pageNumber = 1,
        int pageSize = 20,
        string? search = null,
        string? role = null,
        int? factoryId = null);

    /// <summary>
    /// جلب أعداد المستخدمين حسب الدور (مدير، موظف مصنع، سائق) عبر كل الصفحات —
    /// لتغذية بطاقات الإحصاءات بالأرقام الحقيقية بدلاً من عدّ الصفحة الحالية فقط.
    /// </summary>
    /// <param name="search">نص البحث (اختياري) — يحسب النتائج ضمن نفس سياق البحث المعروض.</param>
    /// <param name="factoryId">معرف المصنع (اختياري) — لعزل البيانات حسب المصنع.</param>
    Task<(int Admins, int FactoryEmployees, int Drivers)> GetRoleCountsAsync(
        string? search,
        int? factoryId);

    Task<UserListItemViewModel?> GetUserByIdAsync(int id);

    Task<ApiResponse<object>> UpdateAsync(int id, UpdateUserViewModel model);

    Task<ApiResponse<object>> DeleteAsync(int id);
}