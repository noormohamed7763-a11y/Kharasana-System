using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Domain.Common;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;
using Kharasana.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kharasana.Infrastructure.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(KharasanaDbContext context)
        : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
    }

    public async Task<User?> GetByPhoneAsync(string phone)
    {
        return await _context.Users.FirstOrDefaultAsync(x => x.Phone == phone);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users.AnyAsync(x => x.Email == email);
    }

    public async Task<bool> PhoneExistsAsync(string phone)
    {
        return await _context.Users.AnyAsync(x => x.Phone == phone);
    }

    // ✅ استعلام مجمّع: كل المعرّفات لمصانع فيها موظف مصنع — استعلام واحد يلغي N+1 في قوائم المصانع
    public async Task<HashSet<int>> GetFactoryIdsWithEmployeeAsync()
    {
        var ids = await _context.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.FactoryEmployee && u.FactoryId.HasValue)
            .Select(u => u.FactoryId!.Value)
            .Distinct()
            .ToListAsync();

        return ids.ToHashSet();
    }

    // ✅ التعديل هنا: دعم استثناء المستخدم الحالي عند التحديث
    public async Task<bool> FactoryHasAccountAsync(int factoryId, int? excludeUserId = null)
    {
        var query = _context.Users.Where(u => u.FactoryId == factoryId && u.Role == UserRole.FactoryEmployee);

        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.UserId != excludeUserId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<(IEnumerable<User> Items, int TotalCount)> GetPagedAsync(
        UserRole? role, int? factoryId, DriverStatus? driverStatus, string? search,
        int pageNumber, int pageSize)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();

        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        if (factoryId.HasValue)
            query = query.Where(u => u.FactoryId == factoryId.Value);

        if (driverStatus.HasValue)
            query = query.Where(u => u.DriverStatus == driverStatus.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var normalizedNameTerm = ArabicTextNormalizer.Normalize(term);
            var phoneDigits = YemeniPhoneHelper.NormalizeForSearch(term);
            var hasPhoneDigits = !string.IsNullOrEmpty(phoneDigits);

            query = query.Where(u =>
                u.FullName
                    .Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا")
                    .Replace("ى", "ي").Replace("ة", "ه")
                    .Contains(normalizedNameTerm)
                || (u.Email != null && u.Email.Contains(term))
                || (hasPhoneDigits && u.Phone != null && u.Phone.Contains(phoneDigits)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}