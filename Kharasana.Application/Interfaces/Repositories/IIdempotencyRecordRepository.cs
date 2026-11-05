using Kharasana.Domain.Entities;

namespace Kharasana.Application.Interfaces.Repositories;

public interface IIdempotencyRecordRepository : IGenericRepository<IdempotencyRecord>
{
    Task<IdempotencyRecord?> GetByKeyAndUserIdAsync(string key, int userId);
}