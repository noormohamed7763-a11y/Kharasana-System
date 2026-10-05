using Kharasana.Domain.Entities;
using Kharasana.Infrastructure.Persistence;
using Kharasana.Application.Interfaces.Repositories;

namespace Kharasana.Infrastructure.Repositories;

public class ActivationTokenRepository : GenericRepository<ActivationToken>, IActivationTokenRepository
{
    public ActivationTokenRepository(KharasanaDbContext context) : base(context)
    {
    }
}