using Kharasana.Domain.Entities;
using Kharasana.Infrastructure.Persistence;
using Kharasana.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kharasana.Infrastructure.Repositories;

public class ActivationTokenRepository : GenericRepository<ActivationToken>, IActivationTokenRepository
{
    public ActivationTokenRepository(KharasanaDbContext context) : base(context)
    {
    }

    public async Task<ActivationToken?> GetByHashAsync(string hash)
    {
        return await _dbSet.FirstOrDefaultAsync(t => t.TokenHash == hash);
    }
}