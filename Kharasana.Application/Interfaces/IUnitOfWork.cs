using Kharasana.Application.Interfaces.Repositories;

namespace Kharasana.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IFactoryRepository Factories { get; }

    IUserRepository Users { get; }

    IConcreteTypeRepository ConcreteTypes { get; }

    IOrderRepository Orders { get; }

    Task<int> SaveChangesAsync();
}