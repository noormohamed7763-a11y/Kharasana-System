using Kharasana.Domain.Entities;

namespace Kharasana.Application.Interfaces.Repositories;

public interface IConcreteTypeRepository : IGenericRepository<ConcreteType>
{
    Task<IEnumerable<ConcreteType>> GetAllWithFactoryAsync();

    Task<IEnumerable<ConcreteType>> GetByFactoryWithFactoryAsync(int factoryId);

    Task<ConcreteType?> GetByIdWithFactoryAsync(int id);
}