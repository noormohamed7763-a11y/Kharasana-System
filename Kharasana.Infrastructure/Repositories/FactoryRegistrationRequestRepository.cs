using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Domain.Entities;
using Kharasana.Infrastructure.Persistence;

namespace Kharasana.Infrastructure.Repositories;

public class FactoryRegistrationRequestRepository : GenericRepository<FactoryRegistrationRequest>, IFactoryRegistrationRequestRepository
{
    public FactoryRegistrationRequestRepository(KharasanaDbContext context) : base(context)
    {
    }
}