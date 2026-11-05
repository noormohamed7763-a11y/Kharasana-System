using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Domain.Entities;
using Kharasana.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kharasana.Infrastructure.Repositories;

public class IdempotencyRecordRepository : GenericRepository<IdempotencyRecord>, IIdempotencyRecordRepository
{
    public IdempotencyRecordRepository(KharasanaDbContext context) : base(context)
    {
    }

    public async Task<IdempotencyRecord?> GetByKeyAndUserIdAsync(string key, int userId)
    {
        return await _dbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Key == key && i.UserId == userId);
    }
}