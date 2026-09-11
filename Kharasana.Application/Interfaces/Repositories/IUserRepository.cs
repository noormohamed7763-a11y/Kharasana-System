using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Interfaces.Repositories;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByPhoneAsync(string phone);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> PhoneExistsAsync(string phone);

    // ✅ التعديل هنا: إضافة المعامل الاختياري لتوافق التطبيق في Repository
    Task<bool> FactoryHasAccountAsync(int factoryId, int? excludeUserId = null);

    /// <summary>
    /// إرجاع مصانع المعرّفات التي تملك حساب موظف مصنع — استعلام واحد دفعةً بدلاً من N استعلامات.
    /// </summary>
    Task<HashSet<int>> GetFactoryIdsWithEmployeeAsync();

    Task<(IEnumerable<User> Items, int TotalCount)> GetPagedAsync(
        UserRole? role, int? factoryId, DriverStatus? driverStatus, string? search,
        int pageNumber, int pageSize);
}